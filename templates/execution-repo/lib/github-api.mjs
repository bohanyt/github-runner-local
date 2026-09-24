import { ProtocolError, REPOSITORY } from './protocol.mjs';

export class GitHubApi {
  constructor(token, repository) {
    if (!token || !REPOSITORY.test(repository) || repository.includes('..'))
      throw new ProtocolError('INVALID_API_CONFIGURATION');
    this.token = token;
    this.repository = repository;
  }

  async request(method, path, body, allow404 = false) {
    const response = await fetch('https://api.github.com' + path, {
      method,
      headers: {
        Authorization: `Bearer ${this.token}`,
        Accept: 'application/vnd.github+json',
        'X-GitHub-Api-Version': '2022-11-28',
        'User-Agent': 'grl-execution-template',
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' })
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(15_000)
    }).catch(() => { throw new ProtocolError('GITHUB_API_UNAVAILABLE'); });
    if (allow404 && response.status === 404) return null;
    if (!response.ok) throw new ProtocolError('GITHUB_API_ERROR');
    return response.status === 204 ? null : response.json();
  }

  async getComment(id) {
    return this.request('GET', `/repos/${this.repository}/issues/comments/${id}`, undefined, true);
  }

  async listComments(issue, since, limit = 200) {
    const output = [];
    for (let page = 1; page <= 2 && output.length < limit; page++) {
      const query = `per_page=100&page=${page}&sort=created&direction=desc&since=${encodeURIComponent(since)}`;
      const batch = await this.request('GET', `/repos/${this.repository}/issues/${issue}/comments?${query}`);
      if (!Array.isArray(batch)) throw new ProtocolError('GITHUB_API_ERROR');
      output.push(...batch);
      if (batch.length < 100) break;
    }
    return output.slice(0, limit);
  }

  async getBranch(branch) {
    const value = await this.request('GET',
      `/repos/${this.repository}/branches/${encodeURIComponent(branch)}`, undefined, true);
    return value ? { sha: value.commit?.sha } : null;
  }

  async compareShaToBranch(sha, branch) {
    const value = await this.request('GET',
      `/repos/${this.repository}/compare/${sha}...${encodeURIComponent(branch)}`, undefined, true);
    return value?.status ?? null;
  }

  async getRun(id, attempt) {
    const run = await this.request('GET',
      `/repos/${this.repository}/actions/runs/${id}/attempts/${attempt}`, undefined, true);
    return run ? { concluded: run.status === 'completed', conclusion: run.conclusion } : null;
  }

  async getRunArtifacts(id) {
    const listing = await this.request('GET',
      `/repos/${this.repository}/actions/runs/${id}/artifacts?per_page=100`);
    if (!Array.isArray(listing?.artifacts)) throw new ProtocolError('GITHUB_API_ERROR');
    return listing.artifacts.map(item => item.name);
  }

  async getCommitStatus(sha, context) {
    const statuses = await this.request('GET',
      `/repos/${this.repository}/commits/${sha}/statuses?per_page=100`, undefined, true);
    const match = Array.isArray(statuses) ? statuses.find(x => x.context === context) : null;
    return match ? { sha, context: match.context, state: match.state,
      target_url: match.target_url } : null;
  }

  async postIssueComment(issue, body) {
    return this.request('POST', `/repos/${this.repository}/issues/${issue}/comments`, { body });
  }

  async createStatus(sha, status) {
    return this.request('POST', `/repos/${this.repository}/statuses/${sha}`, status);
  }
}

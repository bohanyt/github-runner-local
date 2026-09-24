import { statfsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

export function systemAdapter(root = process.cwd()) {
  return {
    async freeDiskGiB() {
      const stat = statfsSync(root, { bigint: true });
      return Number(stat.bavail * stat.bsize) / (1024 ** 3);
    },
    async isElevated() {
      if (process.platform !== 'win32') return process.getuid?.() === 0;
      const groups = execFileSync('whoami.exe', ['/groups'], { encoding: 'utf8', timeout: 5000 });
      return /S-1-16-(?:12288|16384)\b/.test(groups);
    },
    async hasCapability(name) {
      if (name === 'node') return Boolean(process.execPath);
      try {
        execFileSync(process.platform === 'win32' ? 'where.exe' : 'which', [name],
          { stdio: 'ignore', timeout: 5000 });
        return true;
      } catch { return false; }
    }
  };
}

"""Optional browser checks of the offline HTML mock, not Windows acceptance.

Requires Playwright plus an installed compatible Chromium browser.
Loads the source with set_content; does not validate double-click/file navigation.
"""
from pathlib import Path
import argparse
import shutil
from playwright.sync_api import sync_playwright
import json
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--chromium', help='Optional path to an installed Chromium executable')
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
out=root/'local-evidence';out.mkdir(exist_ok=True)
executable=args.chromium or shutil.which('chromium') or shutil.which('chromium-browser')
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True, executable_path=executable)
    page=browser.new_page(viewport={'width':1280,'height':1000})
    errors=[]; network=[]
    page.on('pageerror',lambda err:errors.append(str(err)))
    page.on('request',lambda req: network.append(req.url) if req.url.startswith(('http:','https:')) else None)
    page.set_content((root/'design/wizard-preview.html').read_text(encoding='utf-8'))
    assert page.locator('[data-panel]:visible').count()==1
    assert page.locator('#back').is_disabled()
    page.screenshot(path=str(out/'wizard-welcome.png'),full_page=True)
    for i in range(7):
        page.locator(f'[data-step="{i}"]').click()
        assert page.locator(f'[data-panel="{i}"]').is_visible()
        assert page.locator('[data-panel]:visible').count()==1
    page.locator('[data-step="1"]').click();page.locator('#sign-in').click()
    assert 'simulated account' in page.locator('#account-name').inner_text()
    page.locator('#sign-out').click();assert page.locator('#account-name').inner_text()=='No account connected'
    page.locator('[data-step="3"]').click();page.locator('#machine').fill('<script>example</script>')
    page.locator('[data-step="6"]').click();assert page.locator('#dashboard-name').inner_text()=='<script>example</script>'
    page.locator('[data-step="5"]').click();page.locator('#simulate-fail').click()
    assert 'REPORTING_INCOMPLETE' in page.locator('#verification-status').inner_text()
    page.locator('[data-step="6"]').click()
    assert 'REPORTING_INCOMPLETE' in page.locator('#latest-result').inner_text()
    page.locator('#pause').click();assert 'PAUSED' in page.locator('#runner-status').inner_text()
    page.locator('#disconnect').click();assert 'DISCONNECTED' in page.locator('#runner-status').inner_text()
    page.locator('#next').click();assert page.locator('[data-panel="0"]').is_visible()
    page.set_viewport_size({'width':390,'height':844})
    for i in range(7):
        page.locator(f'[data-step="{i}"]').click()
        assert page.locator(f'[data-panel="{i}"]').is_visible()
        assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth')
    page.locator('[data-step="0"]').click()
    page.screenshot(path=str(out/'wizard-mobile.png'),full_page=True)
    assert not errors,errors
    assert not network,network
    result={'status':'PASS','scope':'offline HTML injected through Playwright set_content; not file-launch acceptance','panels':7,'viewports':['1280x1000','390x844'],
            'tested':['navigation','mock sign-in/reset','user text rendered as text','reporting failure','pause','disconnect','restart','no horizontal overflow'],
            'page_errors':errors,'network_requests':network,'browser':browser.version,
            'not_tested':['Windows','UAC','auth','runner','GitHub job','security isolation']}
    (out/'browser-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2))
    browser.close()

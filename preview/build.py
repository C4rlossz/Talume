"""Rebuild the standalone demonstration from the production UI and the offline fixture."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
web=root/'src/Talume.Web/wwwroot'
def script(text):return '<script>'+text.replace('</script','<\\/script')+'</script>'
seed=json.loads((root/'preview/seed.json').read_text())
html='<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Talume · Prévia interativa</title>'+script((web/'theme.js').read_text())+'<style>'+(web/'app.css').read_text()+'</style></head><body><div id="app"></div><dialog id="modal"><div id="modal-body"></div></dialog><div id="toast" role="status"></div>'
html+=script('window.TALUME_SEED='+json.dumps(seed,ensure_ascii=False)+';window.TALUME_PROPOSAL_CSS='+json.dumps((web/'proposal.css').read_text(),ensure_ascii=False)+';')
html+=script((root/'preview/demo-adapter.js').read_text())+script((web/'app.js').read_text().replace('Ambiente de demonstração · dados fictícios','Prévia interativa · dados fictícios · sem servidor'))+'</body></html>'
(root/'preview/Talume-previa.html').write_text(html)
print('Standalone preview rebuilt.')

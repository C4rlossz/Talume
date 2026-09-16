"""Integration tests against Talume. Python 3, standard library only.
python tests/smoke.py                  # Docker app on localhost:8081 + Mailpit
python tests/smoke.py --local DOTNET   # Starts an isolated SQLite test app and SMTP fixture
"""
import os,sys,json,re,time,subprocess,tempfile,threading,socketserver,email,urllib.request,urllib.error,http.cookiejar
from pathlib import Path
from html import unescape as decode_html
ROOT=Path(__file__).resolve().parents[1]
BASE=os.environ.get('TALUME_TEST_URL','http://127.0.0.1:8081')
MAIL=[]
class SMTP(socketserver.StreamRequestHandler):
 def handle(self):
  self.wfile.write(b'220 localhost ESMTP test\r\n');data=False;buf=[]
  while line:=self.rfile.readline():
   if data:
    if line==b'.\r\n':
     msg=email.message_from_bytes(b''.join(buf));parts=list(msg.walk()) if msg.is_multipart() else [msg];plain=next(p for p in parts if p.get_content_type()=='text/plain');body=plain.get_payload(decode=True).decode(plain.get_content_charset() or 'utf-8',errors='replace');MAIL.append({'to':msg['To'],'body':body,'message':msg});data=False;buf=[];self.wfile.write(b'250 OK\r\n')
    else:buf.append(line)
   elif line.upper().startswith(b'EHLO'):self.wfile.write(b'250-localhost\r\n250 8BITMIME\r\n')
   elif line.upper().startswith(b'DATA'):data=True;self.wfile.write(b'354 End with dot\r\n')
   elif line.upper().startswith(b'QUIT'):self.wfile.write(b'221 Bye\r\n');break
   else:self.wfile.write(b'250 OK\r\n')
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()),urllib.request.ProxyHandler({}));self.token='';self.refresh('/Account')
 def req(self,path,method='GET',body=None,expected=200,csrf=True):
  headers={'Content-Type':'application/json'}
  if csrf:headers['X-CSRF-TOKEN']=self.token
  req=urllib.request.Request(BASE+path,method=method,headers=headers,data=None if body is None else json.dumps(body).encode())
  try:r=self.opener.open(req,timeout=20);status=r.status;text=r.read().decode()
  except urllib.error.HTTPError as e:status=e.code;text=e.read().decode()
  assert status==expected,f'{method} {path}: expected {expected}, got {status}: {text[:500]}'
  try:return json.loads(text)
  except ValueError:return text
 def refresh(self,path='/'):
  html=self.req(path);self.token=re.search(r'name="csrf-token" content="([^"]+)"',html).group(1)
 def login(self,email,password='TalumeDemo2026!'):
  self.req('/api/auth/login','POST',{'email':email,'password':password});self.refresh()
def message(to):
 if '--local' in sys.argv:return next(x['body'] for x in reversed(MAIL) if x['to']==to)
 opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
 messages=json.load(opener.open('http://127.0.0.1:8025/api/v1/messages'))['messages']
 item=next(x for x in messages if any(r['Address']==to for r in x['To']))
 return json.load(opener.open('http://127.0.0.1:8025/api/v1/message/'+item['ID']))['Text']
def logo_png(red=25,green=180,invalid=False):
 import base64,struct,zlib
 def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
 image=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',2,1,8,6,0,0,0))+chunk(b'IDAT',b'not-zlib' if invalid else zlib.compress(bytes([0,red,green,0,255,0,0,0,0])))+chunk(b'IEND',b'')
 return 'data:image/png;base64,'+base64.b64encode(image).decode()
def run():
 c=Client();c.req('/api/projects',expected=401)
 c.req('/api/auth/login','POST',{'email':'freelancer@talume.local','password':'TalumeDemo2026!'},expected=400,csrf=False)
 c.login('freelancer@talume.local');projects=c.req('/api/projects');assert len(projects)>=3
 customer=Client();customer.login('cliente@talume.local');cp=customer.req('/api/projects');assert len(cp)==1
 customer.req('/api/clients',expected=403)
 logo=logo_png();alternate=logo_png(200,30)
 customer.req('/api/business-logo',expected=403)
 customer.req('/api/business-logo','PUT',{'dataUrl':logo},expected=403)
 c.req('/api/business-logo','PUT',{'dataUrl':logo},expected=400,csrf=False)
 c.req('/api/business-logo','PUT',{'dataUrl':'data:image/svg+xml,<svg onload=alert(1) />'},expected=400)
 c.req('/api/business-logo','PUT',{'dataUrl':'data:image/png;base64,AAAA'},expected=400)
 c.req('/api/business-logo','PUT',{'dataUrl':logo_png(invalid=True)},expected=400)
 c.req('/api/business-logo','PUT',{'dataUrl':'data:image/png;base64,'+'A'*700000},expected=400)
 c.req('/api/business-logo','PUT',{'dataUrl':logo})
 assert c.req('/api/business-logo')['dataUrl']==logo
 profile=c.req('/api/business-profile');assert profile['businessName']=='Origins'
 customer.req('/api/business-profile',expected=403)
 customer.req('/api/business-profile','PUT',{'businessName':'Invadida'},expected=403)
 c.req('/api/business-profile','PUT',{'businessName':'','email':'bad'},expected=400)
 c.req('/api/business-profile','PUT',{'businessName':'Origins','website':'javascript:alert(1)'},expected=400)
 profile=c.req('/api/business-profile','PUT',{**profile,'businessName':'Origins','email':'contato@example.com','responsibleName':'Carlos <script>alert(1)</script>'})
 assert c.req('/api/business-profile')['email']=='contato@example.com'
 hidden=next(p for p in projects if p['id']!=cp[0]['id'])
 customer.req('/api/projects/'+hidden['id'],expected=404)
 view=customer.req('/api/projects/'+cp[0]['id']);assert view['tasks']==[] and all(not x['internal'] for x in view['updates'])
 customer.req('/api/projects/'+cp[0]['id'],'PUT',{'stage':'Entregue','dueDate':'2026-12-01','clientPending':''},expected=403)
 stamp=str(int(time.time()));email_address='test-'+stamp+'@example.com'
 new=c.req('/api/clients','POST',{'name':'Cliente <Teste> & Cia','company':'Teste','email':email_address,'phone':'5561999999999','notes':'Nota interna confidencial'})
 service=c.req('/api/services','POST',{'name':'Site de teste','description':'Teste integrado','basePrice':100,'estimatedDays':7})
 quote=c.req('/api/quotes','POST',{'clientId':new['id'],'title':'Teste de orçamento','validUntil':'2026-12-01','discount':10,'terms':'Duas revisões','lines':[{'description':service['name'],'quantity':2,'unitPrice':100}]})
 c.req('/api/services/'+service['id'],'PUT',{'name':'Site atualizado','description':'Mudou preço','basePrice':500,'estimatedDays':9})
 q=next(x for x in c.req('/api/quotes') if x['id']==quote['id']);assert q['total']==190
 customer.req('/Document/'+q['id'],expected=404)
 c.req('/api/business-logo','PUT',{'dataUrl':alternate})
 assert logo in decode_html(c.req('/Document/'+q['id'])) and alternate not in decode_html(c.req('/Document/'+q['id']))
 custom=c.req('/api/quotes','POST',{'clientId':new['id'],'title':'Logo específica','validUntil':'2026-12-01','discount':0,'terms':'','logoDataUrl':alternate,'lines':[{'description':'Teste','quantity':1,'unitPrice':10}]})
 assert alternate in decode_html(c.req('/Document/'+custom['id']))
 no_logo=c.req('/api/quotes','POST',{'clientId':new['id'],'title':'Sem logo','validUntil':'2026-12-01','discount':0,'terms':'','logoDataUrl':'','lines':[{'description':'Teste','quantity':1,'unitPrice':10}]})
 assert 'class="issuer-logo"' not in c.req('/Document/'+no_logo['id'])
 c.req('/api/quotes','POST',{'clientId':new['id'],'title':'Imagem inválida','validUntil':'2026-12-01','discount':0,'terms':'','logoDataUrl':'data:text/html,bad','lines':[{'description':'Teste','quantity':1,'unitPrice':10}]},expected=400)
 c.req('/api/quotes/'+q['id']+'/share','POST',{})
 document=c.req('/Document/'+q['id']);assert 'Duas revis' in document and '190,00' in document and 'Origins' in document and 'contato@example.com' in document and '<script>alert(1)</script>' not in document and '&lt;script&gt;' in document
 project=c.req('/api/quotes/'+q['id']+'/convert','POST',{'dueDate':'2026-12-15'})
 again=c.req('/api/quotes/'+q['id']+'/convert','POST',{'dueDate':'2026-12-15'});assert project['id']==again['id']
 url='/api/projects/'+project['id']
 c.req(url+'/installments','POST',{'label':'Excessiva','amount':191,'dueDate':'2026-12-15'},expected=400)
 c.req(url+'/installments','POST',{'label':'Entrada','amount':95,'dueDate':'2026-12-15'})
 payment=c.req(url)['installments'][0]
 c.req(url+'/installments/'+payment['id']+'/paid','POST',{})
 c.req(url+'/installments/'+payment['id']+'/paid','POST',{})
 assert c.req(url)['installments'][0]['paidAt']
 task=c.req(url+'/tasks','POST',{'title':'Entregar','dueDate':'2026-12-15'})
 c.req(url+'/tasks/'+task['id'],'PUT',{'done':True});assert c.req(url)['tasks'][0]['done']
 c.req(url+'/updates','POST',{'text':'Segredo do freelancer','internal':True})
 c.req(url+'/deliveries','POST',{'title':'Inseguro','url':'javascript:alert(1)'},expected=400)
 c.req(url+'/deliveries','POST',{'title':'Documentação','url':'https://example.com/entrega'})
 c.req(url,'PUT',{'stage':'Em revisão','dueDate':'2026-12-16','clientPending':'Enviar logo'})
 c.req('/api/clients/'+new['id']+'/invite','POST',{})
 if '--local' in sys.argv:
  sent=next(x['message'] for x in reversed(MAIL) if x['to']==email_address)
  htmlpart=next(p for p in sent.walk() if p.get_content_type()=='text/html');html=htmlpart.get_payload(decode=True).decode('utf-8')
  assert 'Cliente &lt;Teste&gt; &amp; Cia' in html and 'Acessar meus projetos' in html and '7 dias' in html
  linked={p.get('Content-ID') for p in sent.walk() if p.get_content_type()=='image/png'}
  assert linked=={'<talume-mark>','<talume-hero>'}
 invitation=message(email_address);token=re.search(r'invite=([A-F0-9]+)',invitation).group(1)
 newuser=Client();newuser.req('/api/auth/invitation?token='+token)
 reg=newuser.req('/api/auth/register','POST',{'name':'Cliente Teste','email':email_address,'password':'NovaSenha123!','invitationToken':token})
 code=re.search(r'\b([0-9]{6})\b',message(email_address)).group(1)
 newuser.req('/api/auth/verify','POST',{'challengeId':reg['challengeId'],'code':'WRONG','newPassword':'NovaSenha123!'},expected=400)
 payload={'challengeId':reg['challengeId'],'code':code,'newPassword':'NovaSenha123!'}
 newuser.req('/api/auth/verify','POST',payload)
 newuser.req('/api/auth/verify','POST',payload,expected=400)
 newuser.login(email_address,'NovaSenha123!')
 data=newuser.req(url);assert data['tasks']==[] and all(not x['internal'] for x in data['updates']) and data['deliveries']
 assert len(newuser.req('/api/quotes'))==1
 assert 'Origins' in newuser.req('/Document/'+q['id']) and logo in decode_html(newuser.req('/Document/'+q['id']))
 newuser.req('/api/projects/'+cp[0]['id'],expected=404)
 newuser.req('/api/auth/logout','POST',{});newuser.refresh('/Account')
 # Reset happens after the issuance cooldown; adjust only fixture clock in SQLite, not application rules.
 if '--local' in sys.argv:
  import sqlite3
  conn=sqlite3.connect(DBPATH);conn.execute('UPDATE EmailChallenges SET ExpiresAt = datetime(ExpiresAt, "-2 minutes")');conn.commit();conn.close()
 else:
  print('Waiting for the email issuance cooldown before password recovery…',flush=True);time.sleep(61)
 reset=newuser.req('/api/auth/forgot','POST',{'email':email_address})
 code=re.search(r'\b([0-9]{6})\b',message(email_address)).group(1)
 newuser.req('/api/auth/verify','POST',{'challengeId':reset['challengeId'],'code':code,'newPassword':'OutraSenha123!'})
 newuser.login(email_address,'OutraSenha123!')
 # An independent freelancer cannot see another freelancer's data.
 other=Client();othermail='freelancer-'+stamp+'@example.com'
 registered=other.req('/api/auth/register','POST',{'name':'Outro freelancer','email':othermail,'password':'OutraSenha123!','invitationToken':None})
 code=re.search(r'\b([0-9]{6})\b',message(othermail)).group(1)
 other.req('/api/auth/verify','POST',{'challengeId':registered['challengeId'],'code':code,'newPassword':'OutraSenha123!'})
 other.login(othermail,'OutraSenha123!');assert other.req('/api/projects')==[] and other.req('/api/clients')==[]
 other.req(url,expected=404)
 assert other.req('/api/business-logo')['dataUrl']==''
 other.req('/api/business-logo','PUT',{'dataUrl':logo,'ownerId':profile['ownerId']})
 assert c.req('/api/business-logo')['dataUrl']==alternate
 c.req('/api/business-logo','PUT',{'dataUrl':''})
 assert logo in decode_html(c.req('/Document/'+q['id']))
 assert other.req('/api/business-profile')['businessName']=='Outro freelancer'
 other.req('/api/business-profile','PUT',{'businessName':'Outra empresa','ownerId':profile['ownerId']})
 assert other.req('/api/business-profile')['businessName']=='Outra empresa'
 assert c.req('/api/business-profile')['businessName']=='Origins'
 other.req('/Document/'+q['id'],expected=404)
 c.req('/api/business-profile','PUT',{**profile,'responsibleName':'Carlos','email':''})
 other.req('/api/quotes','POST',{'clientId':new['id'],'title':'Cruzado','validUntil':'2026-12-01','discount':0,'terms':'','lines':[{'description':'Teste','quantity':1,'unitPrice':1}]},expected=404)
 print('PASS: authorization, tenant isolation, CSRF, quotes/PDF page, validated PNGs, logo ownership and snapshots, HTML email with CID images, idempotent conversion, tasks, payments, links, invitations, OTP single use and password recovery.',flush=True)
 # Capture only original fictional seed data for the standalone visual preview.
 seed_projects=[p for p in c.req('/api/projects') if p['id'] in {x['id'] for x in projects}]
 dump={'me':c.req('/api/me'),'projects':seed_projects,'details':[c.req('/api/projects/'+p['id']) for p in seed_projects],'clients':[x for x in c.req('/api/clients') if x['id']!=new['id']],'services':[s for s in c.req('/api/services') if s['id']!=service['id']],'quotes':[x for x in c.req('/api/quotes') if x['id'] not in {q['id'],custom['id'],no_logo['id']}]}
 if os.environ.get('TALUME_PREVIEW_DATA'):Path(os.environ['TALUME_PREVIEW_DATA']).write_text(json.dumps(dump,ensure_ascii=False))
if __name__=='__main__':
 process=None;server=None
 try:
  if '--local' in sys.argv:
   sdk=sys.argv[sys.argv.index('--local')+1];tmp=tempfile.mkdtemp(prefix='talume-smoke-');DBPATH=tmp+'/test.db'
   server=socketserver.ThreadingTCPServer(('127.0.0.1',0),SMTP);threading.Thread(target=server.serve_forever,daemon=True).start()
   env={**os.environ,'ASPNETCORE_ENVIRONMENT':'Development','ASPNETCORE_URLS':BASE,'Database__Provider':'Sqlite','Database__Initialize':'true','Demo__Seed':'true','ConnectionStrings__Default':'Data Source='+DBPATH,'DataProtection__Path':tmp+'/keys','Mail__Host':'127.0.0.1','Mail__Port':str(server.server_address[1]),'App__BaseUrl':BASE}
   log=open(tmp+'/server.log','w');process=subprocess.Popen([sdk,'run','--no-build','--project',str(ROOT/'src/Talume.Web')],env=env,stdout=log,stderr=log)
   opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
   for _ in range(60):
    try:
     if opener.open(BASE+'/health',timeout=1).status==200:break
    except Exception:time.sleep(.5)
   else:raise RuntimeError('App did not start. '+Path(tmp+'/server.log').read_text()[-2000:])
  run()
 finally:
  if process:process.terminate();process.wait(timeout=10)
  if server:server.shutdown()

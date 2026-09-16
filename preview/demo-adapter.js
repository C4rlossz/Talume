// Offline visual preview only. No authentication, server, email or durable database.
// The production app never loads this adapter. Changes live in memory until reload.
window.TalumeDemo = (()=>{
const db=JSON.parse(JSON.stringify(window.TALUME_SEED));let kind='Freelancer';db.logo='';
db.business={businessName:'Origins',responsibleName:'Carlos',taxId:'',email:'',phone:'',website:'',address:''};
const uid=()=>crypto.randomUUID?crypto.randomUUID():Math.random().toString(36).slice(2);
const viewer=db.clients.find(c=>c.userId),now=()=>new Date().toISOString();
const allowed=id=>kind==='Freelancer'||db.details.some(p=>p.id===id&&p.clientName===viewer.name);
function projects(){return db.details.filter(p=>allowed(p.id)).map(p=>({id:p.id,title:p.title,stage:p.stage,dueDate:p.dueDate,value:p.value,clientName:p.clientName,clientPending:p.clientPending,taskCount:kind==='Freelancer'?p.tasks.length:null,doneCount:kind==='Freelancer'?p.tasks.filter(t=>t.done).length:null,paid:p.installments.filter(i=>i.paidAt).reduce((s,i)=>s+i.amount,0)}));}
function projected(p){return {...p,tasks:kind==='Freelancer'?p.tasks:[],updates:p.updates.filter(u=>kind==='Freelancer'||!u.internal),phone:kind==='Freelancer'?p.phone:null};}
function addProject(b,quoteId=null){const c=db.clients.find(c=>c.id===b.clientId);const p={id:uid(),title:b.title,clientName:c.name,phone:c.phone,stage:'Briefing',dueDate:b.dueDate,value:b.value,quoteId,clientPending:'',tasks:[],updates:[{id:uid(),text:'Projeto iniciado.',internal:false,createdAt:now()}],deliveries:[],installments:[]};db.details.push(p);return {id:p.id};}
async function request(path,method='GET',body={}){
const parts=path.split('/').filter(Boolean);const [resource,id,child,itemId,action]=parts;
if(method==='GET'){
 if(path==='/me')return {displayName:kind==='Freelancer'?'Carlos':viewer.name,email:kind==='Freelancer'?'freelancer@talume.local':viewer.email,kind,demo:true};
 if(path==='/business-logo'){if(kind!=='Freelancer')throw Error('Acesso restrito.');return {dataUrl:db.logo};}
 if(path==='/business-profile'){if(kind!=='Freelancer')throw Error('Acesso restrito.');return {...db.business};}
 if(path==='/projects')return projects();
 if(resource==='projects'&&id){if(!allowed(id))throw Error('Projeto indisponível.');const p=db.details.find(x=>x.id===id);if(!p)throw Error('Projeto não encontrado.');return JSON.parse(JSON.stringify(projected(p)));}
 if(path==='/clients')return db.clients;
 if(path==='/services')return db.services;
 if(path==='/quotes')return db.quotes.filter(q=>kind==='Freelancer'||q.clientName===viewer.name&&q.status!=='Rascunho');
}
if(kind!=='Freelancer')throw Error('O portal do cliente permite apenas consultar os próprios projetos.');
if(resource==='business-logo'&&method==='PUT'){db.logo=body.dataUrl||'';return {dataUrl:db.logo};}
if(resource==='business-profile'&&method==='PUT'){Object.assign(db.business,body);return {...db.business};}
if(resource==='clients'){
 if(child==='invite')throw Error('Na aplicação completa, este botão envia um convite. Abra o projeto com Docker para testar o e-mail.');
 if(method==='PUT'){Object.assign(db.clients.find(c=>c.id===id),body);return {};}
 if(db.clients.some(c=>c.email===body.email))throw Error('Você já cadastrou este e-mail.');
 const c={...body,id:uid(),userId:null};db.clients.push(c);return c;
}
if(resource==='services'){if(method==='PUT'){Object.assign(db.services.find(s=>s.id===id),body);return {};}const s={...body,id:uid()};db.services.push(s);return s;}
if(resource==='quotes'){
 if(child==='share'){db.quotes.find(q=>q.id===id).status='Enviado';return {};}
 if(child==='convert'){const q=db.quotes.find(q=>q.id===id);const existing=db.details.find(p=>p.quoteId===id);if(existing)return {id:existing.id};q.status='Aprovado';return addProject({clientId:q.clientId,title:q.title,value:q.total,dueDate:body.dueDate},q.id);}
 const total=body.lines.reduce((s,l)=>s+l.quantity*l.unitPrice,0)-body.discount;if(total<0)throw Error('Desconto maior que o subtotal.');
 const q={...body,logoDataUrl:body.logoDataUrl??db.logo,total,createdAt:now(),id:uid(),status:'Rascunho',clientName:db.clients.find(c=>c.id===body.clientId).name};db.quotes.push(q);return q;
}
if(resource==='projects'){
 if(!id)return addProject(body);
 const p=db.details.find(p=>p.id===id);
 if(!child){Object.assign(p,body);p.updates.unshift({id:uid(),text:`Projeto atualizado: ${body.stage}. Entrega prevista: ${body.dueDate}.`,internal:false,createdAt:now()});return {};}
 if(child==='tasks'){if(itemId)Object.assign(p.tasks.find(t=>t.id===itemId),body);else p.tasks.push({...body,id:uid(),done:false});return {};}
 if(child==='updates'){p.updates.unshift({...body,id:uid(),createdAt:now()});return {};}
 if(child==='deliveries'){if(!body.url.startsWith('https://'))throw Error('Use um link HTTPS válido.');p.deliveries.push({...body,id:uid(),createdAt:now()});return {};}
 if(child==='installments'){if(action==='paid'){p.installments.find(i=>i.id===itemId).paidAt=now();return {};}
 if(p.installments.reduce((s,i)=>s+i.amount,0)+body.amount>p.value)throw Error('As parcelas não podem superar o valor do projeto.');p.installments.push({...body,id:uid(),paidAt:null});return {};}
}
throw Error('Ação indisponível nesta prévia.');
}
function doc(id,esc,brl){
 const q=db.quotes.find(x=>x.id===id);if(!q||kind!=='Freelancer'&&(q.clientName!==viewer.name||q.status==='Rascunho'))throw Error('Proposta indisponível.');
 const p=db.business,logo=q.logoDataUrl??db.logo,c=db.clients.find(x=>x.id===q.clientId),date=s=>s?new Date(s.slice(0,10)+'T12:00:00').toLocaleDateString('pt-BR'):'—';
 const w=window.open('','_blank');if(!w)throw Error('Permita a abertura de uma nova janela para visualizar a proposta.');
 w.document.write(`<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${esc(p.businessName)} · Proposta comercial</title><style>body{margin:0;background:#f4f6f1;font-family:Arial,sans-serif}.print-toolbar{display:flex}.btn{padding:12px 16px;border:1px solid #cbd5c3;background:#e3efcf;border-radius:8px;cursor:pointer}${window.TALUME_PROPOSAL_CSS}</style></head><body>
 <div class="proposal-toolbar print-toolbar"><p>Prévia com dados fictícios. Escolha “Salvar como PDF” e desative os cabeçalhos e rodapés do navegador.</p><button class="btn" onclick="window.print()">Imprimir / Salvar PDF</button></div>
 <main class="proposal"><header class="proposal-header"><div><span class="proposal-label">PROPOSTA COMERCIAL</span><div class="issuer-brand">${logo?`<img class="issuer-logo" src="${esc(logo)}" alt="Logo de ${esc(p.businessName)}">`:''}<div class="issuer-name">${esc(p.businessName)}</div></div></div><div class="proposal-reference"><span class="proposal-label">REFERÊNCIA</span><strong>${esc(q.id.slice(0,8).toUpperCase())}</strong><small>Emitida em ${date(q.createdAt)}</small></div></header>
 <section class="proposal-intro"><span class="proposal-label">UM PROJETO PARA ${esc(c.company||c.name)}</span><h1>${esc(q.title)}</h1><p>Escopo, investimento e condições para o seu próximo projeto.</p></section>
 <div class="proposal-parties"><section><h2>Prestador do serviço</h2><strong>${esc(p.businessName)}</strong>${p.responsibleName?`<p>Responsável: ${esc(p.responsibleName)}</p>`:''}${p.taxId?`<p>CPF / CNPJ: ${esc(p.taxId)}</p>`:''}${[p.email,p.phone,p.website,p.address].filter(Boolean).map(x=>`<p>${esc(x)}</p>`).join('')}</section><section><h2>Preparada para</h2><strong>${esc(c.company||c.name)}</strong>${c.company?`<p>Aos cuidados de ${esc(c.name)}</p>`:''}<p>${esc(c.email)}</p><p class="proposal-validity">Válida até <b>${date(q.validUntil)}</b></p></section></div>
 <section class="proposal-scope"><h2><span>01</span> Escopo e investimento</h2><table><thead><tr><th>Descrição do serviço</th><th>Qtd.</th><th>Valor unitário</th><th>Valor total</th></tr></thead><tbody>${q.lines.map(l=>`<tr><td>${esc(l.description)}</td><td>${l.quantity}</td><td>${brl(l.unitPrice)}</td><td>${brl(l.quantity*l.unitPrice)}</td></tr>`).join('')}</tbody></table><div class="proposal-totals"><div><span>Subtotal</span><b>${brl(q.lines.reduce((s,l)=>s+l.quantity*l.unitPrice,0))}</b></div><div><span>Desconto</span><b>${brl(q.discount)}</b></div><div class="proposal-grand-total"><span>Investimento total</span><strong>${brl(q.total)}</strong></div></div></section>
 <section class="proposal-terms"><h2><span>02</span> Condições da proposta</h2><p>${esc(q.terms||'Condições a combinar entre prestador e cliente antes da aprovação.')}</p></section><section class="proposal-next"><h2>Vamos dar o próximo passo?</h2><p>Confira os itens e as condições desta proposta. Para confirmar ou solicitar ajustes, entre em contato com ${esc(p.businessName)}.</p></section><footer class="proposal-footer"><div><strong>${esc(p.businessName)}</strong><span>Proposta comercial · Não é nota fiscal.</span></div><div class="proposal-platform">Documento gerado com <b>talume<span>.</span></b></div></footer></main></body></html>`);w.document.close();
}

return {request,switchRole(){kind=kind==='Freelancer'?'Client':'Freelancer';},document:doc};
})();

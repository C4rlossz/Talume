using System.Net;
namespace Talume.Web.Services;

public static class InvitationEmail
{
    public static string Text(string name, string url) => $"Olá, {name}!\n\nSeu projeto tem um espaço no Talume. Crie seu acesso para acompanhar etapas, prazos e materiais dos seus projetos.\n\nAcessar meus projetos:\n{url}\n\nO convite expira em 7 dias.\n\nTalume · Cada etapa, mais perto da entrega.";
    public static string Html(string name, string url, string logo = "cid:talume-mark", string hero = "cid:talume-hero")
    {
        var n = WebUtility.HtmlEncode(name); var link = WebUtility.HtmlEncode(url);
        return $$"""
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="color-scheme" content="light"><title>Seu projeto está no Talume</title></head>
<body style="margin:0;padding:0;background-color:#f2f5ee;font-family:Arial,Helvetica,sans-serif;color:#23392a">
<div style="display:none;max-height:0;overflow:hidden">Olá, {{n}}! Seu convite para acompanhar seus projetos chegou.</div>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f2f5ee"><tr><td align="center" style="padding:32px 12px">
<table role="presentation" width="600" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background-color:#ffffff;border:1px solid #dee6d7;border-radius:18px;overflow:hidden">
<tr><td style="padding:25px 30px;background-color:#213427"><table role="presentation" cellspacing="0" cellpadding="0"><tr><td><img src="{{WebUtility.HtmlEncode(logo)}}" alt="Símbolo do Talume" width="38" height="38" style="display:block;border:0"></td><td style="padding-left:10px;color:#ffffff;font-size:27px;font-weight:bold;letter-spacing:-1px">talume<span style="color:#bce27b">.</span></td></tr></table></td></tr>
<tr><td><img src="{{WebUtility.HtmlEncode(hero)}}" width="600" alt="Seu projeto organizado em etapas: briefing, produção e entrega." style="display:block;width:100%;max-width:600px;height:auto;border:0"></td></tr>
<tr><td style="padding:30px 30px 12px"><p style="margin:0 0 14px;font-size:11px;font-weight:bold;letter-spacing:2px;color:#718263">SEU PORTAL DE PROJETOS</p><h1 style="margin:0 0 16px;font-size:28px;line-height:1.25;font-weight:bold;color:#23392a">Olá, {{n}}!</h1><p style="margin:0 0 13px;font-size:17px;line-height:1.65;color:#23392a">Seu projeto tem um espaço no Talume.</p><p style="margin:0;font-size:15px;line-height:1.75;color:#65715f">Você recebeu um convite para criar seu acesso e acompanhar seus projetos. Confira as etapas, os prazos e os materiais compartilhados pelo seu desenvolvedor, tudo em um só lugar.</p></td></tr>
<tr><td style="padding:20px 30px 24px"><table role="presentation" cellspacing="0" cellpadding="0"><tr><td align="center" bgcolor="#bce27b" style="background-color:#bce27b;border-radius:9px"><a href="{{link}}" style="display:inline-block;padding:16px 24px;border:1px solid #bce27b;border-radius:9px;color:#213427;font-size:15px;font-weight:bold;text-decoration:none">Acessar meus projetos →</a></td></tr></table></td></tr>
<tr><td style="padding:0 30px 26px"><table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td style="background-color:#f0f5e9;border:1px solid #e0e9d5;border-radius:10px;padding:14px 16px;color:#506341;font-size:13px;line-height:1.6"><strong>Seu convite expira em 7 dias.</strong><br>Após esse prazo, solicite um novo convite ao seu desenvolvedor.</td></tr></table></td></tr>
<tr><td style="padding:0 30px 28px"><p style="font-size:12px;line-height:1.7;color:#77816e;margin:0 0 8px">Se o botão não funcionar, copie e cole este link no navegador:</p><a href="{{link}}" style="font-size:12px;line-height:1.8;color:#466b32;word-break:break-all;overflow-wrap:anywhere">{{link}}</a></td></tr>
<tr><td style="padding:20px 30px;border-top:1px solid #e6ecdf"><p style="margin:0;font-size:12px;color:#77816e;line-height:1.7">Talume · Cada etapa, mais perto da entrega.<br>Este convite é pessoal. Se não esperava recebê-lo, pode ignorar esta mensagem.</p></td></tr>
</table></td></tr></table></body></html>
""";
    }
}

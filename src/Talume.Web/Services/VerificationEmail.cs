using System.Net;
namespace Talume.Web.Services;

public static class VerificationEmail
{
    public static string Subject(string purpose) => purpose == "reset" ? "Redefina sua senha no Talume" : "Confirme seu e-mail no Talume";

    public static string Text(string name, string code, string purpose) =>
        $"Olá, {name}!\n\n{Subject(purpose)}\n\nSeu código é {code}.\n\n" +
        (purpose == "reset" ? "Digite este código na tela de recuperação para definir uma nova senha." : "Digite este código na tela de confirmação para ativar seu acesso.") +
        "\n\nEle vale por 10 minutos e pode ser usado uma única vez. Não compartilhe este código.\n\nSe não solicitou, ignore este e-mail.\n\nTalume · Cada etapa, mais perto da entrega.";

    public static string Html(string name, string code, string purpose, string logo = "cid:talume-mark", string hero = "cid:talume-hero")
    {
        var n = WebUtility.HtmlEncode(name);
        var c = WebUtility.HtmlEncode(code);
        var title = Subject(purpose);
        var label = purpose == "reset" ? "RECUPERAÇÃO DE SENHA" : "CONFIRMAÇÃO DE E-MAIL";
        var instruction = purpose == "reset"
            ? "Recebemos um pedido para redefinir sua senha. Digite o código abaixo na tela de recuperação do Talume para escolher uma nova senha."
            : "Falta só confirmar que este e-mail é seu. Digite o código abaixo na tela de confirmação do Talume para ativar seu acesso.";
        return $$"""
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="color-scheme" content="light"><title>{{title}}</title></head>
<body style="margin:0;padding:0;background-color:#f2f5ee;font-family:Arial,Helvetica,sans-serif;color:#23392a">
<div style="display:none;max-height:0;overflow:hidden">Seu código de acesso ao Talume chegou. Ele vale por 10 minutos.</div>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f2f5ee"><tr><td align="center" style="padding:32px 12px">
<table role="presentation" width="600" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background-color:#ffffff;border:1px solid #dee6d7;border-radius:18px;overflow:hidden">
<tr><td style="padding:25px 30px;background-color:#213427"><table role="presentation" cellspacing="0" cellpadding="0"><tr><td><img src="{{WebUtility.HtmlEncode(logo)}}" alt="Símbolo do Talume" width="38" height="38" style="display:block;border:0"></td><td style="padding-left:10px;color:#ffffff;font-size:27px;font-weight:bold;letter-spacing:-1px">talume<span style="color:#bce27b">.</span></td></tr></table></td></tr>
<tr><td><img src="{{WebUtility.HtmlEncode(hero)}}" width="600" alt="Cada etapa, mais perto da entrega: briefing, produção e entrega." style="display:block;width:100%;max-width:600px;height:auto;border:0"></td></tr>
<tr><td style="padding:30px 30px 12px"><p style="margin:0 0 14px;font-size:11px;font-weight:bold;letter-spacing:2px;color:#718263">{{label}}</p><h1 style="margin:0 0 16px;font-size:28px;line-height:1.25;font-weight:bold;color:#23392a">{{title}}</h1><p style="margin:0 0 13px;font-size:17px;line-height:1.65;color:#23392a;overflow-wrap:anywhere">Olá, {{n}}!</p><p style="margin:0;font-size:15px;line-height:1.75;color:#65715f">{{instruction}}</p></td></tr>
<tr><td style="padding:16px 30px 24px"><table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td align="center" bgcolor="#f0f5e9" style="background-color:#f0f5e9;border:1px solid #dce8ce;border-radius:12px;padding:22px 12px"><p style="margin:0 0 12px;font-size:11px;font-weight:bold;letter-spacing:2px;color:#506341">SEU CÓDIGO DE VERIFICAÇÃO</p><p style="margin:0;font-family:'Courier New',monospace;font-size:36px;line-height:1.3;font-weight:bold;letter-spacing:6px;color:#213427;white-space:nowrap">{{c}}</p><p style="margin:12px 0 0;font-size:12px;line-height:1.5;color:#65715f">Válido por 10 minutos · Uso único</p></td></tr></table></td></tr>
<tr><td style="padding:0 30px 26px"><p style="margin:0;font-size:13px;line-height:1.7;color:#506341"><strong>Não compartilhe este código.</strong><br>Se ele expirar, solicite um novo na tela do Talume.</p></td></tr>
<tr><td style="padding:20px 30px;border-top:1px solid #e6ecdf"><p style="margin:0;font-size:12px;color:#77816e;line-height:1.7">Talume · Cada etapa, mais perto da entrega.<br>Se você não fez esta solicitação, pode ignorar esta mensagem.</p></td></tr>
</table></td></tr></table></body></html>
""";
    }
}

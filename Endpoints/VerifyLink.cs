using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/verify-link")]
    protected static async Task<IResponse> HandleVerifyLink(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
        var uid = req.Query.GetOrThrow("user");
        var code = req.Query.GetOrThrow("code");
        var user = await req.UserTable.GetByIdAsync(uid);
        if (user.MailToken == null)
            return new RedirectResponse(req.RedirectUrl);
        if (await req.UserTable.VerifyMailAsync(user.Id, code, req))
            return new Page(
                req, false,
                PluginTexts.Verified,
                [
                    new Section(
                        PluginTexts.Verified,
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph(PluginTexts.VerifiedIntro)
                                ]
                            )
                        ]
                    )
                ]
            );
        else
            return new Page(
                req, false,
                PluginTexts.InvalidLink,
                [
                    new Section(
                        PluginTexts.InvalidLink,
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph(PluginTexts.InvalidLinkIntro)
                                ]
                            )
                        ]
                    )
                ]
            );
    }
}
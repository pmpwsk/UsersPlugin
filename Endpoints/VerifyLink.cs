using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleVerifyLink(Request req)
    {
        req.ForceGET();
        var uid = req.Query.GetOrThrow("user");
        var code = req.Query.GetOrThrow("code");
        var user = await req.UserTable.GetByIdAsync(uid);
        if (user.MailToken == null)
            return new RedirectResponse(req.RedirectUrl);
        if (await req.UserTable.VerifyMailAsync(user.Id, code, req))
            return new Page(
                req, false,
                "Verified",
                [
                    new Section(
                        "Verified",
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph("You have successfully verified your email address.")
                                ]
                            )
                        ]
                    )
                ]
            );
        else
            return new Page(
                req, false,
                "Invalid link",
                [
                    new Section(
                        "Invalid link",
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph("This email verification link is invalid.")
                                ]
                            )
                        ]
                    )
                ]
            );
    }
}
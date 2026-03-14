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
        var page = new Page(req, false);
        if (await req.UserTable.VerifyMailAsync(user.Id, code, req))
        {
            page.Title = "Verified";
            page.Sections.Add(new(
                "Verified",
                [
                    new Subsection(
                        null,
                        [
                            new Paragraph("You have successfully verified your email address.")
                        ]
                    )
                ]
            ));
        }
        else
        {
            page.Title = "Invalid link";
            page.Sections.Add(new(
                "Invalid link",
                [
                    new Subsection(
                        null,
                        [
                            new Paragraph("This email verification link is invalid.")
                        ]
                    )
                ]
            ));
        }
                
        return page;
    }
}
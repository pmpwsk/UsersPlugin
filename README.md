# UsersPlugin
Plugin for [WebFramework](https://github.com/pmpwsk/WebFramework) that adds a simple interface to register, log in and manage your account settings.

Website: https://uwap.org/projects/users-plugin

Changelog: https://uwap.org/changes/users-plugin

## Main features
- Registering as a user (with mail verification)
- Logging in as a user (with 2FA)
- Logging out, logging all other session out
- Managing users (only as an administrator)
- Setting your theme
- Changing your username, email address and password
- Managing your account's two-factor authentication
- Scheduling your account for deletion
- Account recovery (forgotten username or password)

## Installation
You can add this plugin to your WF project by installing the NuGet package: [uwap.UsersPlugin](https://www.nuget.org/packages/uwap.UsersPlugin/)

You can also download the source code and reference it in your project file.

Once installed, add the following things to your program start code:
- Add <code>using uwap.WebFramework.Plugins;</code> to the top, otherwise you have to prepend it to <code>UsersPlugin</code>
- Create a new object of the plugin: <code>UsersPlugin usersPlugin = new();</code>
- Map the plugin to a path of your choosing (like any/account): <code>PluginManager.Map("any/account", usersPlugin);</code>

You can do all that with a single line of code before starting the WF server:<br/><code>PluginManager.Map("any/account", new uwap.WebFramework.Plugins.UsersPlugin());</code>

If you map the plugin to a path that isn't <code>/account</code>, you need to change the following properties in <code>AccountManager.Settings</code> accordingly:<br /><code>TwoFactorPath = "/account/2fa"</code><br /><code>MailVerifyPath = "/account/verify"</code><br /><code>LoginAllowedPaths = new[] {"/account/logout"}</code><br /><code>LoginPath = "/account/login"</code>

## Plans for the future
- New theme settings (design+background+accent+font instead of fully preset themes)
- Making the worker delete accounts that are past their date of scheduled deletion
- More options for user management
- Notification settings
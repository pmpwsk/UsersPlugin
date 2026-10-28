using uwap.WebFramework.Localization;
using static uwap.WebFramework.Localization.CommonTexts;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    public static class PluginTexts
    {
        public static readonly Translation RegisterInstead = Create(
            "bi bi-person-vcard",
            "Register instead",
            "Stattdessen registrieren"
        );
        
        public static readonly Translation RegisterInsteadDescription = Create(
            "Don't have an account yet?",
            "Haben Sie noch kein Konto?"
        );
        
        public static readonly Translation LogInInstead = Create(
            "bi bi-lock",
            "Log in instead",
            "Stattdessen anmelden"
        );
        
        public static readonly Translation LogInInsteadDescription = Create(
            "Already have an account?",
            "Haben Sie schon ein Konto?"
        );
        
        public static readonly Translation LogOutInstead = Create(
            "bi bi-box-arrow-left",
            "Log out instead",
            "Stattdessen abmelden"
        );
        
        public static readonly Translation SetUpLater = Create(
            "Set up your account later.",
            "Richten Sie Ihr Konto später ein."
        );
        
        public static readonly Translation ChangeMail = Create(
            "bi bi-arrow-left-right",
            "Change email address",
            "Email-Adresse ändern"
        );
        
        public static readonly Translation ChangeMailDescription = Create(
            "Try another email address.",
            "Probieren Sie eine andere Email-Adresse."
        );
        
        public static readonly Translation LogInLater = Create(
            "Log in later.",
            "Melden Sie sich später an."
        );
        
        public static readonly Translation LogoutIntro = Create(
            "Are you sure you want to log out?",
            "Wollen Sie sich wirklich abmelden?"
        );
        
        public static readonly Translation LogoutNoDescription = Create(
            "Stay logged in.",
            "Bleiben Sie angemeldet."
        );
        
        public static readonly Translation WrongLogin = Create(
            "The combination of username and password you have entered isn't correct.",
            "Die angegebene Kombination aus Benutzername und Passwort ist falsch."
        );
        
        public static readonly Translation NewLogin = Create(
            "New login",
            "Neuer Login"
        );
        
        public static readonly Translation NewLoginMail = Create(
            "Someone just successfully logged into your account.",
            "Jemand hat sich soeben erfolgreich mit Ihrem Konto angemeldet."
        );
        
        public static readonly Translation WrongAuthWithout2FA = Create(
            "The provided password is invalid.",
            "Das angegebene Passwort ist falsch."
        );
        
        public static readonly Translation WrongAuthWith2FA = Create(
            "The provided password or 2FA code is invalid.",
            "Das angegebene Passwort oder der 2FA-Code ist falsch."
        );
        
        public static readonly Translation Verify = Create(
            "bi bi-envelope",
            "Verify",
            "Bestätigen"
        );
        
        public static readonly Translation Verified = Create(
            "bi bi-check-circle",
            "Verified",
            "Bestätigt"
        );
        
        public static readonly Translation VerifiedIntro = Create(
            "You have successfully verified your email address.",
            "Sie haben erfolgreich Ihre Email-Adresse bestätigt."
        );
        
        public static readonly Translation InvalidLink = Create(
            "bi bi-x-circle",
            "Invalid link",
            "Ungültiger Link"
        );
        
        public static readonly Translation InvalidLinkIntro = Create(
            "This email verification link is invalid.",
            "Dieser Link zur Email-Verifizierung ist ungültig."
        );
        
        public static readonly Translation VerifyIntro = Create(
            "You should have received an email containing a verification code, please enter it below to finish setting up your account.",
            "Sie sollten eine Email mit einem Bestätigungscode erhalten haben, bitte geben Sie diesen unten ein, um Ihr Konto einzurichten."
        );
        
        public static readonly Translation TwoFactorLong = Create(
            "bi bi-phone",
            "Two-factor authentication",
            "Zwei-Faktor-Authentifizierung"
        );
    
        public static readonly Translation PasswortResetAlert = Create(
            "A password reset has been requested and a corresponding link has been sent to your email address.",
            "Ein Link zum Passwort-Zurücksetzen wurde angefordert und an Ihre Email-Adresse versendet."
        );
        
        public static readonly Translation ManageUsers = Create(
            "bi bi-people",
            "Manage users",
            "Nutzer verwalten"
        );
        
        public static readonly Translation ManageUsersDescription = Create(
            "Control all existing users.",
            "Verwalten Sie alle registrierten Nutzer."
        );
        
        public static readonly Translation SettingsDescription = Create(
            "Control your account details.",
            "Ändern Sie Ihre Konto-Einstellungen."
        );
        
        public static readonly Translation LogoutOthers = Create(
            "bi bi-slash-circle",
            "Log out all other devices",
            "Andere Geräte ausloggen"
        );
        
        public static readonly Translation LogoutOthersIntro = Create(
            "Are you sure you want to log out all other browsers and all applications with partial access?",
            "Wollen Sie wirklich alle anderen Browser und Anwendungen mit Teilzugriff abmelden?"
        );
        
        public static readonly Translation Applications = Create(
            "bi bi-hdd-rack",
            "Applications",
            "Anwendungen"
        );
        
        public static readonly Translation ApplicationsIntro = Create(
            "These are the applications that have partial access to your account.",
            "Hier finden Sie die Anwendungen, die einen Teilzugriff auf Ihr Konto haben."
        );
        
        public static readonly Translation AllowedPaths = Create(
            "Allowed paths",
            "Erlaubte Pfade"
        );
        
        public static readonly Translation All = Create(
            "All",
            "Alle"
        );
        
        public static readonly Translation NoApplications = Create(
            "There are currently no applications that have partial access to your account.",
            "Aktuell hat keine Anwendung einen Teilzugriff auf Ihr Konto."
        );
        
        public static readonly Translation ReallyRemoveApplication = Create(
            "Do you really want to remove this application's partial access to your account?",
            "Möchten Sie wirklich den Teilzugriff dieser Anwendung auf Ihr Konto entfernen?"
        );
    
        public static readonly Translation DeleteAccount = Create(
            "bi bi-trash",
            "Delete account",
            "Konto löschen"
        );
    
        public static readonly Translation DeleteAccountIntro1 = Create(
            "We're very sad to see you go! If you're leaving because you've been experiencing issues, please let us know and we'll try our best to fix it. The goal of this project is to make your experience as nice as possible.",
            "Schade, dass Sie uns verlassen! Falls Sie wegen Problemen am System gehen möchten, bitte teilen Sie uns diese Probleme mit, wir kümmern uns bestmöglich darum. Das Ziel dieses Projektes ist es, eine möglichst angenehme Erfahrung zu bieten."
        );
        
        public static readonly Translation DeleteAccountIntro2Without2FA = Create(
            "If you really want to delete your account, enter your password below.",
            "Falls Sie Ihr Konto wirklich löschen möchten, geben Sie unten ihr Passwort ein."
        );
        
        public static readonly Translation DeleteAccountIntro2With2FA = Create(
            "If you really want to delete your account, enter your password and 2FA code below.",
            "Falls Sie Ihr Konto wirklich löschen möchten, geben Sie unten ihr Passwort und ihren 2FA-Code ein."
        );
        
        public static readonly Translation DeleteAccountSad = Create(
            "bi bi-emoji-frown",
            "Delete",
            "Löschen"
        );
        
        public static readonly Translation AccountDeleted = Create(
            "Account deleted",
            "Konto gelöscht"
        );
        
        public static readonly Translation AccountDeletedMail = Create(
            "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.",
            "Sie haben soeben die Löschung Ihres Konto beantragt. Wir werden Ihre Daten für 30 Tage behalten, falls Sie sich umentscheiden. Falls Sie Ihr Konto wiederherstellen möchten, melden Sie sich einfach innerhalb der nächsten 30 Tage an. Falls die Daten sofort gelöscht werden sollen, kontaktieren Sie bitte den Support, indem Sie auf diese Email antworten."
        );
        
        public static readonly Translation NewPassword = Create(
            "bi bi-key",
            "New password",
            "Neues Passwort"
        );
        
        public static readonly Translation NewPasswordPlaceholder = Create(
            "Enter a new password...",
            "Neues Passwort eingeben..."
        );
        
        public static readonly Translation NewPasswordAgain = Create(
            "bi bi-key",
            "Confirm password",
            "Passwort bestätigen"
        );
        
        public static readonly Translation NewPasswordAgainPlaceholder = Create(
            "Enter the password again...",
            "Passwort erneut eingeben..."
        );
        
        public static readonly Translation NewPasswordsUnequal = Create(
            "The passwords do not match.",
            "Die Passwörter stimmen nicht überein."
        );
        
        public static readonly Translation PasswordChanged = Create(
            "Password changed",
            "Passwort geändert"
        );
        
        public static readonly Translation PasswordChangedMail = Create(
            "Your password was just changed.",
            "Ihr Passwort wurde soeben geändert."
        );
    
        public static readonly Translation AccountRecovery = Create(
            "bi bi-life-preserver",
            "Account recovery",
            "Kontowiederherstellung"
        );
    
        public static readonly Translation AccountRecoveryDescription = Create(
            "Can't access your account?",
            "Können Sie sich nicht anmelden?"
        );
    
        public static readonly Translation AccountRecoveryIntro = Create(
            "Here are some options to recover your account. If you still can't log in, please contact our support.",
            "Hier sind die Optionen, um Ihr Konto wieder zu erreichen. Falls Sie sich weiterhin nicht anmelden können, kontaktieren Sie bitte den Support."
        );
    
        public static readonly Translation UsernameRecovery = Create(
            "bi bi-person",
            "Username finding",
            "Benutzername finden"
        );
    
        public static readonly Translation UsernameRecoveryDescription = Create(
            "Receive an email containing your username.",
            "Email mit Ihrem Benutzernamen erhalten."
        );
    
        public static readonly Translation UsernameRecoveryIntro = Create(
            "Enter your email address below and you'll receive an email telling you your username.",
            "Geben Sie Ihre Email-Adresse ein, um eine Email mit Ihrem Benutzernamen zu erhalten."
        );
    
        public static readonly Translation UsernameRecoveryMail = Create(
            "You requested your username, it is:",
            "Sie haben Ihren Nutzernamen angefordert, dieser lautet:"
        );
    
        public static readonly Translation PasswordRecovery = Create(
            "bi bi-key",
            "Password reset",
            "Passwort zurücksetzen"
        );
    
        public static readonly Translation PasswordRecoveryDescription = Create(
            "Receive an email to reset your password.",
            "Email mit einem Link zum Passwort-Zurücksetzen erhalten."
        );
    
        public static readonly Translation PasswordRecoveryIntro = Create(
            "Enter your email address below and you'll receive an email with a link to reset your password.",
            "Geben Sie Ihre Email-Adresse ein, um eine Email mit einem Link zum Passwort-Zurücksetzen zu erhalten."
        );
    
        public static readonly Translation PasswordRecoverySetIntro = Create(
            "Enter a new password and confirm it below.",
            "Geben Sie ein neues Passwort ein und bestätigen Sie es darunter."
        );
    
        public static readonly Translation PasswordRecoveryMail1 = Create(
            "You requested password recovery. Open the following link to reset your password:",
            "Sie haben eine Passwort-Zurücksetzung angefordert. Öffnen Sie den folgenden Link, um ein neues Passwort zu setzen:"
        );
    
        public static readonly Translation PasswordRecoveryMail2 = Create(
            "You can cancel the password reset in the account panel.",
            "Sie können die Zurücksetzung im Konto-Menü abbrechen."
        );
        
        public static readonly Translation PasswordRecoveredMail = Create(
            "Your password was just changed by recovery.",
            "Ihr Passwort wurde soeben über die Kontowiederherstellung geändert."
        );
    
        public static readonly Translation TwoFactorRecovery = Create(
            "bi bi-phone",
            "2FA recovery",
            "2FA wiederherstellen"
        );
    
        public static readonly Translation TwoFactorRecoveryDescription = Create(
            "Information about 2FA recovery.",
            "Informationen zu 2FA-Wiederherstellung."
        );
        
        public static readonly Translation TwoFactorRecoveryIntro = Create(
            "If you've lost access to your 2FA app, you can use one of the recovery codes you were given when you enabled 2FA. If you've lost those as well, please contact our support.",
            "Falls Sie den Zugang zu Ihrer 2FA-App verloren haben, nutzen Sie einen der Backup-Codes der Ihnen gegeben wurde, als sie 2FA aktiviert haben. Falls Sie diese auch verloren haben, kontaktieren Sie bitte den Support."
        );
        
        public static readonly Translation UsernameSettingsDescription = Create(
            "Change your username.",
            "Ändern Sie Ihren Benutzernamen."
        );
        
        public static readonly Translation MailSettingsDescription = Create(
            "Change your email address.",
            "Ändern Sie Ihre Email-Adresse."
        );
        
        public static readonly Translation PasswordSettingsDescription = Create(
            "Change your password.",
            "Ändern Sie Ihr Passwort."
        );
        
        public static readonly Translation EnableTwoFactor = Create(
            "bi bi-phone",
            "Enable 2FA",
            "2FA aktivieren"
        );
        
        public static readonly Translation EnableTwoFactorDescription = Create(
            "Enable 2FA.",
            "Aktivieren Sie 2FA."
        );
        
        public static readonly Translation EnableTwoFactorIntro = Create(
            "Two-factor authentication is disabled. Follow the steps below to enable it.",
            "Zwei-Faktor-Authentifizierung ist nicht aktiv. Folgen Sie den Schritten unten, um es zu aktivieren."
        );
        
        public static readonly Translation PrivateKey = Create(
            "bi bi-upc-scan",
            "Private key",
            "Privater Schlüssel"
        );
        
        public static readonly Translation PrivateKeyIntro = Create(
            "First, scan the QR code using your authenticator app or manually enter the private key below it.",
            "Scannen Sie als Erstes den QR-Code mit ihrer 2FA-App oder geben Sie den privaten Schlüssel manuell ein."
        );
        
        public static readonly Translation RecoveryCodes = Create(
            "bi bi-life-preserver",
            "Recovery codes",
            "Backup-Codes"
        );
        
        public static readonly Translation RecoveryCodesIntro = Create(
            "Next, copy these recovery codes or download them as a file. They can be used like single-use 2FA codes in case you lose access to your authenticator app, so keep them safe.",
            "Speichern Sie als nächstes diese Backup-Codes an einem sicheren Ort. Diese können wie einmalig nutzbare 2FA-Codes genutzt werden, falls Sie den Zugang zu Ihrer 2FA-App verlieren."
        );
        
        public static readonly Translation Confirmation = Create(
            "bi bi-check-circle",
            "Confirmation",
            "Bestätigung"
        );
        
        public static readonly Translation ConfirmationIntro = Create(
            "Finally, enter your password and the current code shown by your 2FA app.",
            "Geben Sie zuletzt Ihr Passwort und den aktuell angezeigten Code der 2FA-App ein."
        );
        
        public static readonly Translation TwoFactorEnabled = Create(
            "2FA enabled",
            "2FA aktiviert"
        );
        
        public static readonly Translation TwoFactorEnabledMail = Create(
            "Two-factor authentication has just been enabled.",
            "Zwei-Faktor-Authentifizierung wurde soeben aktiviert."
        );
        
        public static readonly Translation DisableTwoFactor = Create(
            "Disable 2FA",
            "2FA deaktivieren"
        );
        
        public static readonly Translation DisableTwoFactorDescription = Create(
            "bi bi-phone",
            "Disable 2FA.",
            "Deaktivieren Sie 2FA."
        );
        
        public static readonly Translation DisableTwoFactorIntro = Create(
            "Two-factor authentication is enabled. Enter your password and current 2FA code below to disable it. If you've lost access to your 2FA app, you can also enter one of your recovery codes.",
            "Zwei-Faktor-Authentifizierung ist aktiv. Geben Sie unten Ihr passwort und den aktuellen 2FA-Code ein, um es zu deaktivieren. Falls Sie den Zugang zu Ihrer 2FA-App verloren haben, können Sie auch einen Ihrer Backup-Codes eingeben."
        );
        
        public static readonly Translation TwoFactorDisabled = Create(
            "2FA disabled",
            "2FA deaktiviert"
        );
        
        public static readonly Translation TwoFactorDisabledMail = Create(
            "Two-factor authentication has just been disabled.",
            "Zwei-Faktor-Authentifizierung wurde soeben deaktiviert."
        );
        
        public static readonly Translation ManageApplicationsDescription = Create(
            "Manage apps that can access your account.",
            "Verwalten Sie die Anwendungen mit Teilzugriff auf Ihr Konto."
        );
        
        public static readonly Translation DeleteAccountDescription = Create(
            "Close your account.",
            "Löschen Sie Ihr Konto."
        );
        
        public static readonly Translation MailChange = Create(
            "Email change",
            "Email-Änderung"
        );
        
        public static readonly Translation MailChangeRequested1 = Create(
            "You requested to change your email to \"",
            "Sie haben die Änderung Ihrer Email-Adresse zu \""
        );
        
        public static readonly Translation MailChangeRequested2 = Create(
            "\". Please enter the verification code provided in the email we sent to that address here.",
            "\" beantragt. Bitte geben Sie den Verifizierungscode ein, der an diese Email-Adresse gesendet wurde."
        );
        
        public static readonly Translation Resend = Create(
            "bi bi-envelope",
            "Resend",
            "Neu senden"
        );
        
        public static readonly Translation CodeSent = Create(
            "The code has been sent.",
            "Der Code wurde versendet."
        );
        
        public static readonly Translation MailChangeMail = Create(
            "You requested to change your email address to this address. Your verification code is:",
            "Sie haben die Änderung Ihrer Email-Adresse zu dieser Adresse beantragt. Ihr Verifizierungscode lautet:"
        );
        
        public static readonly Translation MailVerificationCode = Create(
            "Verification code",
            "Verifizierungscode"
        );
        
        public static readonly Translation CodePlaceholder = Create(
            "Enter the code...",
            "Code eingeben..."
        );
        
        public static readonly Translation CodeInvalid = Create(
            "The provided code is invalid.",
            "Der angegebene Code ist falsch."
        );
        
        public static readonly Translation MailChanged = Create(
            "Email changed",
            "Email geändert"
        );
        
        public static readonly Translation MailChangedMail1 = Create(
            "Your email was just changed to \"",
            "Ihre Email-Adresse wurde soeben zu \""
        );
        
        public static readonly Translation MailChangedMail2 = Create(
            "\".",
            "\" geändert."
        );
        
        public static readonly Translation NewMail = Create(
            "bi bi-envelope",
            "New email address",
            "Neue Email-Adresse"
        );
    
        public static readonly Translation NewMailPlaceholder = Create(
            "Enter your new email address...",
            "Neue Email-Adresse eingeben..."
        );
        
        public static readonly Translation NewUsername = Create(
            "bi bi-person",
            "New username",
            "Neuer Benutzername"
        );
    
        public static readonly Translation NewUsernamePlaceholder = Create(
            "Enter a username...",
            "Benutzernamen eingeben..."
        );
        
        public static readonly Translation UsernameChanged = Create(
            "Username changed",
            "Benutzername geändert"
        );
        
        public static readonly Translation UsernameChangedMail1 = Create(
            "Your username was just changed to \"",
            "Ihr Benutzername wurde soeben zu \""
        );
        
        public static readonly Translation UsernameChangedMail2 = Create(
            "\".",
            "\" geändert."
        );
        
        public static readonly Translation ThisStaysLoggedIn = Create(
            "This device will stay logged in.",
            "Dieses Gerät bleibt angemeldet."
        );
        
        public static readonly Translation AllStayLoggedIn = Create(
            "All devices will remain logged in.",
            "Alle Geräte bleiben angemeldet."
        );
        
        public static readonly Translation OthersStayLoggedIn = Create(
            "Other devices will remain logged in.",
            "Andere Geräte bleiben angemeldet."
        );
        
        public static readonly Translation WarningOthersStayLoggedIn = Create(
            "Warning: Other devices will remain logged in.",
            "Achtung: Andere Geräte bleiben angemeldet."
        );
        
        public static readonly Translation Welcome = Create(
            "Welcome",
            "Willkommen"
        );
        
        public static readonly Translation WelcomeMail1 = Create(
            "Thank you for registering on",
            "Vielen Dank für Ihre Registrierung bei"
        );
        
        public static readonly Translation WelcomeMail2 = Create(
            "To verify your email address, click",
            "Um Ihre Email-Adresse zu bestätigen, klicken Sie"
        );
        
        public static readonly Translation WelcomeMail3 = Create(
            "or enter the following code:",
            "oder geben Sie folgenden Code ein:"
        );
        
        public static readonly Translation Here = Create(
            "here",
            "hier"
        );
    }
}
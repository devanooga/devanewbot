# devanewbot

Rewrite of devanoobot in C#

## Local Development

1. Clone this repo
2. Go to "Your Apps" on the [Slack API site](https://api.slack.com/apps)
3. Click the green "Create New App" button.
4. Give the app a unique name (such as "devanoobot-brb3-dev"), and select the appropriate workspace.
5. Click "Create App".
6. Back in the source code, copy the `appsettings.json` file to `appsettings.local.json`.
7. From your app settings in Slack, copy the "Signing Secret" to `Slack:SigningSecret` in your
`appsettings.local.json`.
8. Copy the "Verification Token" to `Slack:VerificationToken` in your `appsettings.local.json`.
9. From the left hand navigation, select "Oauth & Permissions".
10. Scroll down to "Scopes" and add the Scopes needed for developing the feature you are working on.
11. Scroll back to the top and click "Install App to Workspace", then follow the instructions for installing the
application.
12. Copy the provided Oauth token to `Slack:OauthToken` in your `appsettings.local.json`.
13. From the left hand navigation, select "Socket Mode". Click "Enable Socket Mode" and create a key.
14. Copy the key from the previous step to `SlackSocket:AppToken` in your `appsettings.local.json`.

You are now ready to build and run the application.
Work on changes in a branch, and create a PR when you are ready to merge.
Please note in your PR what scopes are used by your feature, and any Slack App configuration needs to be made.
Also, be careful not to commit your `appsettings.local.json` into the repository.

## Working with Commands

When creating new Commands in Slack, be sure to add a unique suffix to the command to prevent collisions with
the production devanewbot. For example, when working on the `/stallman` command, I configure my Slack app to have
a `/stallman-brb3` command. In my `appsettings.local.json`, I add `-brb3` to `SlackSocket::CommandSuffix`.
This allows the CommandService to route Socket messages to the correct command.

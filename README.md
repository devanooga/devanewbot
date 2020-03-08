# devanewbot

Rewrite of devanoobot in C#

## Goals

- Replace current devanoobot functionality
- Connect to reddit for automated posting to /r/devanooga
- Implement custom `/command`s

## Development

1. Clone this repo
2. Go to "Your Apps" on the [Slack API page site](https://api.slack.com/apps)
3. Click the green "Create New App" button.
4. Give the app a unique name (such as "devanoobot-brb3-dev"), and select the appropriate workspace.
5. Click "Create App".
6. Back in the source code, copy the `appsettings.json` file to `appsettings.Development.json`.
7. From your app settings in Slack, copy the "Signing Secret" to `Slack:SigningSecret` in your `appsettings`.
8. Copy the "Verification Token" to `Slack:VerificationToken` in your `appsettings`.
9. From the left hand navigation, select "Oauth & Permissions".
10. Scroll down to "Scopes" and add the Scopes needed for developing the feature you are working on.
11. Scroll back to the top and click "Install App to Workspace", then follow the instructions for installing the
application.
12. Copy the provided Oauth token to `Slack:OauthToken` in your `appsettings`.

You are now ready to build and run the application.
Work on changes in a branch, and create a PR when you are ready to merge.

## Working with Webhooks

If your feature needs access to webhooks, you will need to install [ngrok](https://ngrok.com/).
A sample ngrok configuration can be found in the `ngrok.sample.yml` file.
Be sure to update the authtoken and subdomain to match your configuration.

Webhooks should verify the validity of the Slack webhook using `SlackDotNet.Slack.ValidWebhookMessage()` before acting
on them.

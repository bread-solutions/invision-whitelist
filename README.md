# Invison Community Whitelist 🍞
## Basic Group Based Whitelisting for Invision Community

## Installation
---
Make sure you've downloaded the latest release of the resource from the Github repo! [Releases](https://github.com/bread-solutions/invision-whitelist/releases)
To install the resource, drag the `ips-whitelist` folder inside your resources folder, and add the following to your server.cfg:
`ensure ips-whitelist` 

## Configuration
---
To configure the invision whitelist resource, navigate to the `config.json` file of the resource

Place your Invision Community API Link in the `"apiBaseURL"` field, (The API url is the default url followed by `/api`, for example: `https://example.com/api`)

Then place your **API token** in the `"apiToken"` field (read below for information regarding generating API tokens)

Finally, list all of your desired whitelisted website group ID's in the `"allowedGroupIds"` field, (**Format:** ["123", "456"])
>If a member on your IPS community has one of the corrosponding groups, it will whitelist the user.

## Generating an API Key
---
To generate an API key, navigate to your community's admin panel, then in the `System` tab, navigate to `API > Rest API Keys > Create New`
Once on the page prompting you to create a REST API key, fill out all required fields, then navigate to `Endpoint Permissions >  System > Members` and allow access to `GET` requests for `core/members`.

## Further Help
---
If you're looking for further help join the [Bread Solutions](https://discord.gg/JckpxefJzu) Discord server

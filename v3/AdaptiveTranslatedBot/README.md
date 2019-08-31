# Multilingual Bot

Bot Framework v3 multilingual bot - adaptive card translation sample

This bot has been created using [Bot Framework](https://dev.botframework.com), it shows how to translate incoming and outgoing text using a custom middleware and the [Microsoft Translator Text API](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/).

## Concepts introduced in this sample

The message is first received by RootDialog, and from there the logic just got propagated to custom API [TranslateAdaptiveCardAsync](https://github.com/PurnaChandraPanda/AdaptiveCardBotTranslate/blob/master/v3/AdaptiveTranslatedBot/AdaptiveTranslate/Dialogs/RootDialog.cs#L212), which actually helped in translation adaptive card content/ attachment.

The [Microsoft Translator Text API](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/), Microsoft Translator Text API is a cloud-based machine translation service. With this API you can translate text in near real-time from any app or service through a simple REST API call.
The API uses the most modern neural machine translation technology, as well as offering statistical machine translation technology.

## Prerequisites

- [.NET Framework SDK](https://dotnet.microsoft.com/download) version 4.7.2

- [Microsoft Translator Text API key](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-text-how-to-signup)

    To consume the Microsoft Translator Text API, first obtain a key following the instructions in the [Microsoft Translator Text API documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-text-how-to-signup).

    Paste the key in the `TranslatorKey` setting in the `web.config` file.


## To try this sample

- Clone the repository

- In a terminal, navigate to the v3 project directory
- Run the bot from Visual Studio.

  - Launch Visual Studio
  - File -> Open -> Project/Solution
  - Navigate to `v3\AdaptiveTranslatedBot` folder
  - Select `AdaptiveTranslatedBot.sln` file
  - Press `F5` to run the project

## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.3.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:3978/api/messages`

### Conversion logic

It's being hard-coded to `fr` converting locale - [here](https://github.com/PurnaChandraPanda/AdaptiveCardBotTranslate/blob/master/v3/AdaptiveTranslatedBot/AdaptiveTranslate/Dialogs/RootDialog.cs#L208). However, it can always be optimized and make more dynamic.

### Microsoft Translator Text API

The [Microsoft Translator Text API](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/), Microsoft Translator Text API is a cloud-based machine translation service. With this API you can translate text in near real-time from any app or service through a simple REST API call.
The API uses the most modern neural machine translation technology, as well as offering statistical machine translation technology.

### Add `trns:APIKey` to Application Settings

If you used the `web.config` file to store your `trns:APIKey` then you'll need to add this key and its value to the Application Settings for your deployed bot.

- Log into the [Azure portal](https://portal.azure.com)
- In the left nav, click on `Bot Services`
- Click the `<your_bot_name>` Name to display the bot's Web App Settings
- Click the `Application Settings`
- Scroll to the `Application settings` section
- Click `+ Add new setting`
- Add the key `trns:APIKey` with a value of the Translator Text API `Authentication key` created from the steps above


# StardewDiscord
> Relays messages from Stardew Valley to Discord via webhooks using SMAPI

[![GitHub release](https://img.shields.io/github/release/steven-kraft/StardewDiscord.svg)](https://github.com/steven-kraft/StardewDiscord/releases)

![](example.gif)

## Installation

* Install [SMAPI](https://smapi.io/)
* Extract latest release to mods folder
* Create a new [webhook](https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks) on Discord
* Open config.json in a text editor
* Replace `FARM NAME` with the name of your farm. (case sensitive, exclude the word "farm")
* Replace `DISCORD URL` with the webhook URL provided by Discord

#### Adding/Changing Emojis

The `emojis.json` file translates the emoji ids in Stardew Valley to an equivalent Discord emoji. Modify this file if you want to add your own emojis or you aren't satisfied with the current emojis.

## Contributing

1. Fork it (<https://github.com/steven-kraft/StardewDiscord/fork>)
2. Create your feature branch (`git checkout -b feature/fooBar`)
3. Commit your changes (`git commit -am 'Add some fooBar'`)
4. Push to the branch (`git push origin feature/fooBar`)
5. Create a new Pull Request

[![ko-fi](https://www.ko-fi.com/img/donate_sm.png)](https://ko-fi.com/O5O7QSZT)

## License

Distributed under the MIT license. See ``LICENSE`` for more information.

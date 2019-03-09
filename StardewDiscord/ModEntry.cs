using System.Collections.Generic;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Flurl.Http;
using Newtonsoft.Json;
using System.IO;

namespace StardewDiscord
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
            string emojiFile = Path.Combine(helper.DirectoryPath, "emojis.json");
            loadEmojis(emojiFile);
            string settingsFile = Path.Combine(helper.DirectoryPath, "config.json");
            loadSettings(settingsFile);
        }
        private IReflectedField<List<ChatMessage>> messagesField;
        int msgCount;
        private int lastMsg = 0;
        private Dictionary<int, string> emojis;
        private Settings settings;

        Dictionary<string, string> special_char = new Dictionary<string, string>() { { "=", "☆" }, { "$", "⭗" }, { "*", "💢" }, { "@", "◁" }, { "<", "♡" }, { ">", "▷" } };

        struct Settings
        {
            public Dictionary<string, string> farms { get; set; }
        }

        private string replaceSpecialChar(string msg)
        {
            foreach (string s in special_char.Keys) {
                msg = msg.Replace(s, special_char[s]);
            }
            return msg;
        }

        /// <summary>Loads settings from config file.</summary>
        /// <param name="file">Name of config file.</param>
        private void loadSettings(string file)
        {
            string json = File.ReadAllText(file);
            settings = JsonConvert.DeserializeObject<Settings>(json);
        }

        /// <summary>Sends message to a Discord channel via webhook.</summary>
        /// <param name="msg">Message to be sent</param>
        /// <param name="farm">Name of farm</param>
        /// <param name="notification">Indicates whether message should be treated as a notification</param>
        private async Task SendMessage(string msg, string farm, bool notification = false)
        {
            if (!settings.farms.ContainsKey(farm))
                return;

            msg = replaceSpecialChar(msg);
            string url = settings.farms[farm];
            if (notification)
            {
                object data = new { embeds = new List<object> { new { description = $"**{msg}**", color = 16098851 } } };
                var responseString = await url.PostJsonAsync(data);
            }
            else
            {
                var responseString = await url.PostUrlEncodedAsync(new { content = msg }).ReceiveString();
            }
        }

        /// <summary>Loads emoji aliases from config file.</summary>
        /// <param name="file">Name of config file.</param>
        private void loadEmojis(string file)
        {
            string json = File.ReadAllText(file);
            emojis = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
        }

        /// <summary>Returns a list of active players.</summary>
        private List<string> getPlayers()
        {
            List<string> players = new List<string>();
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                players.Add(farmer.name.ToString());
            }
            return players;
        }

        /// <summary>Raised once every second.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            string temp = Game1.player.farmName;

            messagesField = Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages");
            msgCount = messagesField.GetValue().Count;
            if (msgCount == 0) { return; }
            if (messagesField.GetValue()[msgCount - 1].GetHashCode() != lastMsg)
            {
                lastMsg = messagesField.GetValue()[msgCount - 1].GetHashCode();
                
                string msg = "";
                foreach(ChatSnippet m in messagesField.GetValue()[msgCount - 1].message)
                {
                    msg += m.message;
                    if (m.message == null)
                    {
                        if (emojis.ContainsKey(m.emojiIndex))
                        {
                            if (emojis[m.emojiIndex] != "") { msg += $":{emojis[m.emojiIndex]}:"; }
                            else { Monitor.Log($"Emoji {m.emojiIndex} does not have an associated Discord emoji", LogLevel.Info); }
                        }
                        else { Monitor.Log($"Could not find emoji with index {m.emojiIndex}", LogLevel.Info); }
                    }
                }
                if (messagesField.GetValue()[msgCount - 1].color == Color.Yellow && msg.IndexOf(">  -") != 0) {
                    SendMessage(msg, Game1.player.farmName, true);
                }
                else if (getPlayers().Contains(messagesField.GetValue()[msgCount - 1].message[0].message.Split(':')[0]))
                {
                    SendMessage(msg, Game1.player.farmName);
                }
            }
        }
    }
}
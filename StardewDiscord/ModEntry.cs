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
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
            helper.Events.GameLoop.Saved += this.OnSaved;
            string emojiFile = Path.Combine(helper.DirectoryPath, "emojis.json");
            LoadEmojis(emojiFile);
            string settingsFile = Path.Combine(helper.DirectoryPath, "config.json");
            LoadSettings(settingsFile);
        }
        private IReflectedField<List<ChatMessage>> messagesField;
        int msgCount;
        private int lastMsg = 0;
        private Dictionary<int, string> emojis;
        private Settings settings;
        List<ChatMessage> messages = new List<ChatMessage>();

        Dictionary<string, string> special_char = new Dictionary<string, string>() { { "=", "☆" }, { "$", "⭗" }, { "*", "💢" }, { "@", "◁" }, { "<", "♡" }, { ">", "▷" } };

        struct Settings
        {
            public Dictionary<string, string> farms { get; set; }
        }

        private string ReplaceSpecialChar(string msg)
        {
            foreach (string s in special_char.Keys) {
                msg = msg.Replace(s, special_char[s]);
            }
            return msg;
        }

        /// <summary>Loads settings from config file.</summary>
        /// <param name="file">Name of config file.</param>
        private void LoadSettings(string file)
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

            msg = ReplaceSpecialChar(msg);
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
        private void LoadEmojis(string file)
        {
            string json = File.ReadAllText(file);
            emojis = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
        }

        /// <summary>Returns a list of active players.</summary>
        private List<string> GetPlayers()
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
        private async void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            foreach(ChatMessage message in messages)
            {
                string msg = "";
                foreach (ChatSnippet m in message.message)
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
                if (message.color == Color.Yellow && msg.IndexOf(">  -") != 0)
                {
                    await SendMessage(msg, Game1.player.farmName, true);
                }
                else if (GetPlayers().Contains(message.message[0].message.Split(':')[0]))
                {
                    await SendMessage(msg, Game1.player.farmName);
                }
            }
            messages.Clear();
        }

        /// <summary>Raised once every tick.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            messagesField = Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages");
            msgCount = messagesField.GetValue().Count;
            if (msgCount == 0) { return; }

            ChatMessage message = messagesField.GetValue()[msgCount - 1];
            if (message.GetHashCode() != lastMsg)
            {
                lastMsg = message.GetHashCode();
                messages.Add(message);
            }
        }

        /// <summary>Raised once after game is saved.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaved(object sender, SavedEventArgs e)
        {
            messagesField = Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages");
            msgCount = messagesField.GetValue().Count;
            if (msgCount == 0) { return; }

            int count = 1;
            ChatMessage message = messagesField.GetValue()[msgCount - count];
            while (message.GetHashCode() != lastMsg)
            {
                messages.Add(message);
                message = messagesField.GetValue()[msgCount - ++count];
            }
            lastMsg = messagesField.GetValue()[msgCount - 1].GetHashCode();
            messages.Reverse();
        }
    }
}
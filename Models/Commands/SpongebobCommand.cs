using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace devanewbot.Models.Commands
{
    public class SpongebobCommand : SlashCommand
    {
        public string Response()
        {
            var rand = new Random();
            var text = "";
            foreach (var c in Text)
            {
                text += rand.Next(2) == 0 ? c.ToString().ToUpper() : c.ToString().ToLower();
            }
            return text + " :spongebobmock:";
        }
    }
}
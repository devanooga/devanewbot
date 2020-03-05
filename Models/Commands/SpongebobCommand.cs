using System;
using System.Linq;

namespace devanewbot.Models.Commands
{
    public class SpongebobCommand : SlashCommand
    {
        public string Response()
        {
            // spONgEbOB cASe THE TexT
            Text = new string(Text.Select(c => Spongebobify(c)).ToArray());

            return $"{Text} :spongebobmock:";
        }

        /// <summary>
        /// Takes char and randomly converts it to uppercase or lowercase
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private char Spongebobify(char c)
        {
            var rand = new Random();
            return rand.Next(2) == 0 ? Char.ToUpper(c) : Char.ToLower(c);
        }
    }
}
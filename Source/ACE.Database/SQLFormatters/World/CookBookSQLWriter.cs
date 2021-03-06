using System;
using System.Collections.Generic;
using System.IO;

using ACE.Database.Models.World;

namespace ACE.Database.SQLFormatters.World
{
    public class CookBookSQLWriter : SQLWriter
    {
        /// <summary>
        /// Default is formed from: input.RecipeId.ToString("00000")
        /// </summary>
        public string GetDefaultFileName(CookBook input)
        {
            string fileName = input.RecipeId.ToString("00000");
            fileName = IllegalInFileName.Replace(fileName, "_");
            fileName += ".sql";

            return fileName;
        }

        public void CreateSQLDELETEStatement(IList<CookBook> input, StreamWriter writer)
        {
            foreach (var value in input)
                writer.WriteLine($"DELETE FROM `cook_book` WHERE `recipe_Id` = {value.RecipeId};");
        }

        public void CreateSQLINSERTStatement(IList<CookBook> input, StreamWriter writer)
        {
            writer.WriteLine("INSERT INTO `cook_book` (`recipe_Id`, `source_W_C_I_D`, `target_W_C_I_D`)");

            var lineGenerator = new Func<int, string>(i =>
            {
                string sourceLabel = null;
                if (WeenieNames != null)
                    WeenieNames.TryGetValue(input[i].SourceWCID, out sourceLabel);

                string targetLabel = null;
                if (WeenieNames != null)
                    WeenieNames.TryGetValue(input[i].TargetWCID, out targetLabel);

                return $"{input[i].RecipeId}, {input[i].SourceWCID} /* {sourceLabel} */, {input[i].TargetWCID.ToString().PadLeft(5)} /* {targetLabel} */)";
            });

            ValuesWriter(input.Count, lineGenerator, writer);
        }
    }
}

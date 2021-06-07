// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Wrapper.TF.Common
{
    public static class CatalogUtil
    {
        // regexes with different new line symbols
        private const string CATALOG_ITEM_PATTERN = @"item {{{0}  name: ""(?<name>.*)""{0}  id: (?<id>\d+){0}  display_name: ""(?<displayName>.*)""{0}}}";
        private static readonly string s_catalog_item_pattern_env = string.Format(CultureInfo.InvariantCulture, CATALOG_ITEM_PATTERN, Environment.NewLine);
        private static readonly string s_catalog_item_pattern_unix = string.Format(CultureInfo.InvariantCulture, CATALOG_ITEM_PATTERN, "\n");

        /// <summary>
        /// Reads catalog of well-known objects from text file.
        /// </summary>
        /// <param name="file">path to the text file</param>
        /// <returns>collection of items</returns>
        public static IEnumerable<CatalogItem> ReadCatalogItems(string file)
        {
            using FileStream stream = File.OpenRead(file);
            using StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            Regex regex = new Regex(s_catalog_item_pattern_env);
            var matches = regex.Matches(text);
            if (matches.Count == 0)
            {
                regex = new Regex(s_catalog_item_pattern_unix);
                matches = regex.Matches(text);
            }

            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var id = int.Parse(match.Groups[2].Value);
                var displayName = match.Groups[3].Value;

                yield return new CatalogItem()
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                };
            }
        }
    }

    public class CatalogItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }
}

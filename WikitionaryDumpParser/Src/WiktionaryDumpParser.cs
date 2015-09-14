using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.SharpZipLib.BZip2;
using WikitionaryDumpParser.Src;

namespace WiktionaryDumpParser.Src
{
    public class WiktionaryDumpParser
    {

        public TranslatedEntities ExtractTranslatedEntities(string dumpFilePath, string srcLanguage, string tgtLanguage)
        {
            var entities = new TranslatedEntities()
            {
                SrcLanguage = srcLanguage,
                TgtLanguage = tgtLanguage,
                Entities = new List<TranslatedEntity>()
            };

            using(var inputFileStream = File.OpenRead(dumpFilePath))
            {
                using (var decompressedStream = new BZip2InputStream(inputFileStream))
                {
                    var reader = XmlReader.Create(decompressedStream);

                    while (reader.ReadToFollowing("page"))
                    {
                        var foundTitle = reader.ReadToDescendant("title");
                        if (foundTitle)
                        {
                            var title = reader.ReadInnerXml();
                            var foundRevision = reader.ReadToNextSibling("revision");
                            if (foundRevision)
                            {
                                var foundText = reader.ReadToDescendant("text");
                                if (foundText)
                                {
                                    var text = reader.ReadInnerXml();
                                    var translationEntries = ExtractTranslatedEntitiesFromPageContent(text, tgtLanguage, title);

                                    entities.Entities.AddRange(translationEntries);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Couldn't find title in node");
                        }
                    } 
                }
	        }

            return entities;
        }

        /// <summary>
        /// Extracts the translations for a given target language in a wiktionary word page content
        /// </summary>
        /// <param name="text">The text content of the page</param>
        /// <param name="targetLanguage">The target language (several translation languages are available in a given page)</param>
        /// <param name="title">The title of the page (ie, the word in the source language)</param>
        /// <returns>The collection of translated entities</returns>
        private List<TranslatedEntity> ExtractTranslatedEntitiesFromPageContent(string text, string targetLanguage, string title)
        {
            if (string.IsNullOrEmpty(text)) { return new List<TranslatedEntity>(); }

            // match translations section
            var sections = new List<TextSection>();
            const string sectionPattern = "\\={2,}[\\w\\s]+\\={2,}";
            var sectionMatches = Regex.Matches(text, sectionPattern);
            for (var i = 0; i < sectionMatches.Count; i++)
            {
                var sectionMatch = sectionMatches[i];
                sections.Add(new TextSection(sectionMatch));
            }

            // match translation synset definitions
            // {{trans-see|exit}}, {{trans-top|style of living}}
            var synsetSections = new List<SynsetSection>();
            const string translationSynsetPattern = "\\{\\{((check)?trans-(top|see)(-also)?|ttbc-top)(\\|)?([^\\}]+)?\\}\\}";
            var translationSynsetMatches = Regex.Matches(text, translationSynsetPattern);
            for (var i = 0; i < translationSynsetMatches.Count; i++)
            {
                var translationSynsetMatch = translationSynsetMatches[i];
                synsetSections.Add(new SynsetSection(translationSynsetMatch));
            }

            // match translations
            // {{t+check|fr|bistoquet|m}} / {{t-check|ee|dɔla}} / {{t-simple|fr|mouiller}} / {{t-|fr|gouille|f}}
            var translationEntries = new List<TranslatedEntity>();
            var translationPattern = "\\{\\{t(r)?(\\+|\\-)?(check|simple)?\\|" + targetLanguage +"\\|[^\\}]+\\}\\}";
            var translationMatches = Regex.Matches(text, translationPattern);
            for (var i = 0; i < translationMatches.Count; i++)
            {
                var match = translationMatches[i];
                var matchValue = match.Value;
                var parts = matchValue.Split(new[] {'{', '}', '|'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2)
                {
                    var lang = parts[1];
                    var name = parts[2];

                    var synsetSection = synsetSections.LastOrDefault(section => section.Index < match.Index);
                    if (synsetSection != null)
                    {
                        var translationSection = sections
                            .LastOrDefault(section => section.Name == "Translations" 
                                && section.StartIndex < synsetSection.Index);
                        if (translationSection != null)
                        {
                            var posSection = sections
                                .LastOrDefault(section => section.Level == translationSection.Level - 1
                                                          && section.StartIndex < translationSection.StartIndex);
                            if (posSection != null)
                            {
                                translationEntries.Add(new TranslatedEntity()
                                {
                                    Definition = synsetSection.Synset,
                                    SrcName = title,
                                    TgtName = name,
                                    Pos = posSection.Name
                                });
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Cannot extract translations from page '{0}'", title);
                    }
                }
                else
                {
                    Console.WriteLine("Unsupported translation match value: " + matchValue);
                }
            }

            return translationEntries;
        }
    }
}

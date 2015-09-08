using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using WikitionaryDumpParser.Src;

namespace WiktionaryDumpParser.Src
{
    public class WiktionaryDumpParser
    {

        public List<WiktionaryEntry> ParseDumpFile(string dumpFilePath, string sourceLanguage, List<string> targetLanguages, string outputFilePath)
        {
            var entries = new List<WiktionaryEntry>();

            using(var inputFileStream = File.OpenRead(dumpFilePath))
            {
                var reader = XmlReader.Create(inputFileStream);

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
                                var translationEntries = ExtractPosTranslations(text, targetLanguages)
                                    .Select(ent => string.Format("{0}|{1}|{2}|{3}|{4}|{5}", title, sourceLanguage, ent.Language, ent.Name, ent.Pos, ent.Synset))
                                    .ToList();
                                File.AppendAllLines(outputFilePath, translationEntries);
                                
                                /*var posTranslations = ExtractPosTranslations(text, targetLanguages);
                                if (posTranslations != null && posTranslations.Any())
                                {
                                    var entry = new WiktionaryEntry()
                                                            {
                                                                Name = title,
                                                                Language = sourceLanguage,
                                                                PosTranslations = posTranslations
                                                            };
                                    entries.Add(entry); 
                                }*/
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Couldn't find title in node");
                    }
                }
	        }

            return entries;
        }

        private List<TranslationEntry> ExtractPosTranslations(string text, List<string> targetLanguages)
        {
            if (string.IsNullOrEmpty(text)){ return new List<TranslationEntry>(); }

            // match translations section
            var sections = new List<TextSection>();
            var sectionPattern = "\\={2,}[\\w\\s]+\\={2,}";
            var sectionMatches = Regex.Matches(text, sectionPattern);
            for (var i = 0; i < sectionMatches.Count; i++)
            {
                var sectionMatch = sectionMatches[i];
                sections.Add(new TextSection(sectionMatch));
            }

            // match translation synset definitions
            // {{trans-see|exit}}, {{trans-top|style of living}}
            var synsetSections = new List<SynsetSection>();
            var translationSynsetPattern = "\\{\\{((check)?trans-(top|see)(-also)?|ttbc-top)(\\|)?([^\\}]+)?\\}\\}";
            var translationSynsetMatches = Regex.Matches(text, translationSynsetPattern);
            for (var i = 0; i < translationSynsetMatches.Count; i++)
            {
                var translationSynsetMatch = translationSynsetMatches[i];
                synsetSections.Add(new SynsetSection(translationSynsetMatch));
            }

            // match translations
            var translationEntries = new List<TranslationEntry>();
            var translationPattern = "\\{\\{t(?!erm)[^\\|\\}]*\\|" + string.Join("|", targetLanguages) +"\\|[^\\}]+\\}\\}";
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
                                translationEntries.Add(new TranslationEntry()
                                {
                                    Name = name,
                                    Language = lang,
                                    Pos = posSection.Name,
                                    Synset = synsetSection.Synset
                                });
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Couldn't link {0}-{1} to its info in {2}", lang, name, text);
                    }
                }
                else
                {
                    Console.WriteLine("Unsupported translation match value: " + matchValue);
                }
            }

            /*var posTranslations = translationEntries
                .GroupBy(ent => ent.Pos)
                .Select(grp => new PosTranslations()
                {
                    Pos = grp.Key,
                    SynsetTranslations = grp.GroupBy(ent => ent.Synset)
                        .Select(synGrp => new SynsetTranslation()
                        {
                            Definition = synGrp.Key,
                            Translations = synGrp.Select(ent => new Translation()
                            {
                                Language = ent.Language,
                                Name = ent.Name
                            })
                            .ToList()
                        })
                        .ToList()
                })
                .ToList();
            return posTranslations;*/
            return translationEntries;
        }
    }
}

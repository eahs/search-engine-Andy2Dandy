using System.Diagnostics;
using System.Runtime.CompilerServices;
using RoverSearch.Models;
using System.IO;          
using System.Linq;        
using System.Collections.Generic; 
using System;

namespace RoverSearch.Services;

public class SearchService
{
    private string path = @".\Data";

    public SearchService()
    {
   
    }
    
    

    /// <summary>
    /// Enhanced search implementation with case-insensitivity, word-based matching, and ranking.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public SearchResults Search(string query)
    {
        var sw = new Stopwatch();
        sw.Start();

        var results = new List<Result>();

        // punctuation standardization
        var searchTerms = query
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', '\r', '\n', ':', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        if (!searchTerms.Any())
        {
            return new SearchResults { Query = query, Results = results, Duration = sw.Elapsed };
        }

        // Handle the case where the Data directory might not exist during debugging
        if (!Directory.Exists(path))
        {
            sw.Stop();
            // In a real application, you'd log this error.
            return new SearchResults { Query = query, Results = results, Duration = sw.Elapsed };
        }

        foreach (string file in Directory.GetFiles(path))
        {
            var content = File.ReadAllText(file);
            
            var normalizedContent = content.ToLowerInvariant();

            int score = 0;
            bool allTermsFound = true;

            //search ranking
            foreach (var term in searchTerms)
            {
                
                if (!normalizedContent.Contains(term))
                {
                    allTermsFound = false;
                    break;
                }

                
                score += CountOccurrences(normalizedContent, term);
            }

            if (!allTermsFound) continue;

            
            var metadata = ExtractMetadata(content);

            var result = new Result
            {
                Filename = Path.GetFileName(file),
                Title = metadata.Title,
                Description = metadata.Description,
                Score = score,
                //rest of file
                Snippet = content.Substring(metadata.HeaderLength).Trim()
            };

            results.Add(result);
        }

        sw.Stop();

        // 4. Improve Search Relevance Sort by Score
        var rankedResults = results.OrderByDescending(r => r.Score).ToList();

        return new SearchResults
        {
            Query = query,
            Results = rankedResults,
            Duration = sw.Elapsed
        };
    }
    
 
    private int CountOccurrences(string text, string term)
    {
        int count = 0;
        int i = 0;
        
        while ((i = text.IndexOf(term, i, StringComparison.Ordinal)) != -1)
        {
            i += term.Length;
            count++;
        }
        return count;
    }
    
    private (string Title, string Description, int HeaderLength) ExtractMetadata(string content)
    {
        string title = "N/A";
        string description = "No description provided.";
        int headerLength = 0;
        int descriptionLinesToTake = 2; 
        int linesTakenForDescription = 0;

        using (var reader = new StringReader(content))
        {
            string line;
            bool inHeader = true;

            while ((line = reader.ReadLine()) != null)
            {
                // Track the characters read, including the newline
                headerLength += line.Length + Environment.NewLine.Length;

                if (inHeader)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        
                        inHeader = false;
                        continue; 
                    }

                    // Look for the title (case-insensitive)
                    if (line.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                    {
                        title = line.Substring("title:".Length).Trim();
                    }
                }
                else 
                {
                    if (!string.IsNullOrWhiteSpace(line) && linesTakenForDescription < descriptionLinesToTake)
                    {
                        
                        if (linesTakenForDescription > 0)
                        {
                            description += " / "; 
                        }
                        description += line.Trim();

                        linesTakenForDescription++;
                    }

                    if (linesTakenForDescription >= descriptionLinesToTake)
                    {
                        
                        int headerEndIndex = content.IndexOf(title, StringComparison.OrdinalIgnoreCase);
                        if (headerEndIndex >= 0)
                        {
                            
                            headerEndIndex = content.IndexOf(Environment.NewLine + Environment.NewLine, headerEndIndex);
                            if (headerEndIndex >= 0)
                            {
                                // The length is up to and including the double newline
                                headerLength = headerEndIndex + (2 * Environment.NewLine.Length);
                            }
                        }
                        break;
                    }
                }
            }
        }

        return (title, description, headerLength);
    }
}
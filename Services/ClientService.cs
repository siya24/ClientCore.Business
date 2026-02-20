
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ClientCore.Business.Services
{
    public class ClientService(IMapper mapper, ClientCoreDBContext context) : IClientService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ClientCoreDBContext _context = context;


        public async Task<string> AddAsync(CreateClientDTO createClientDTO)
        {
            if (createClientDTO is null)
            {
                throw new Exception("client cannot be null");
            }

            try
            {
                var client = _mapper.Map<Client>(createClientDTO);
                client.Id = Guid.NewGuid().ToString();
                client.Code = await GenerateClientCodeAsync(createClientDTO.Name);
                await _context.Clients.AddAsync(client);
                await _context.SaveChangesAsync();
                return client.Id;
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                throw new Exception("An error occurred while adding the client.", ex);
            }
        }

        private async Task<string> GenerateClientCodeAsync(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                throw new ArgumentException("Client name cannot be empty");

            // Generate alpha part from client name (taking first letters from words)
            string alphaPart = GenerateAlphaPart(clientName);

            // Find the next available numeric suffix
            int numericSuffix = await FindNextAvailableNumericSuffixAsync(alphaPart);

            // Format the numeric part as 3 digits with leading zeros
            string numericPart = numericSuffix.ToString("D3");

            return alphaPart + numericPart;
        }

        private static string GenerateAlphaPart(string clientName)
        {
            // Split the name into words
            string[] words = clientName.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            StringBuilder alphaPart = new StringBuilder();

            if (words.Length >= 3)
            {
                // Take first letter from first 3 words
                for (int i = 0; i < 3; i++)
                {
                    if (words[i].Length > 0 && char.IsLetter(words[i][0]))
                    {
                        alphaPart.Append(char.ToUpperInvariant(words[i][0]));
                    }
                    else
                    {
                        // If word starts with non-letter, use 'A' as fallback
                        alphaPart.Append('A');
                    }
                }
            }
            else if (words.Length == 2)
            {
                // Take first letter from first 2 words
                for (int i = 0; i < 2; i++)
                {
                    if (words[i].Length > 0 && char.IsLetter(words[i][0]))
                    {
                        alphaPart.Append(char.ToUpperInvariant(words[i][0]));
                    }
                    else
                    {
                        alphaPart.Append('A');
                    }
                }

                // Need one more character - take from the second word if possible
                string secondWord = words[1];
                if (secondWord.Length > 1 && char.IsLetter(secondWord[1]))
                {
                    alphaPart.Append(char.ToUpperInvariant(secondWord[1]));
                }
                else
                {
                    // Find next letter in second word or pad
                    char nextChar = FindNextLetterInWord(secondWord, 1) ?? 'A';
                    alphaPart.Append(nextChar);
                }
            }
            else if (words.Length == 1)
            {
                string word = words[0];

                // Take first 3 letters from the single word
                for (int i = 0; i < 3; i++)
                {
                    if (i < word.Length && char.IsLetter(word[i]))
                    {
                        alphaPart.Append(char.ToUpperInvariant(word[i]));
                    }
                    else if (i < word.Length)
                    {
                        // Current character is not a letter, find next letter
                        char? nextLetter = FindNextLetterInWord(word, i);
                        alphaPart.Append(nextLetter ?? 'A');
                    }
                    else
                    {
                        // Pad with letters starting from A
                        alphaPart.Append((char)('A' + (i - word.Length)));
                    }
                }
            }

            return alphaPart.ToString();
        }

        private static char? FindNextLetterInWord(string word, int startIndex)
        {
            for (int i = startIndex; i < word.Length; i++)
            {
                if (char.IsLetter(word[i]))
                {
                    return char.ToUpperInvariant(word[i]);
                }
            }
            return null;
        }

        private async Task<int> FindNextAvailableNumericSuffixAsync(string alphaPart)
        {
            // Get all existing codes with this alpha part from the database
            var existingCodes = await _context.Clients
                .Where(c => c.Code != null && c.Code.StartsWith(alphaPart))
                .Select(c => c.Code)
                .ToListAsync();

            int numericSuffix = 1;
            bool foundUnique = false;

            while (!foundUnique)
            {
                string testCode = alphaPart + numericSuffix.ToString("D3");

                if (!existingCodes.Contains(testCode))
                {
                    foundUnique = true;
                }
                else
                {
                    numericSuffix++;
                }
            }

            return numericSuffix;
        }

        public Task<List<GetClientDTO>> GetAllAsync()
        {
            throw new NotImplementedException();
        }
    }
}

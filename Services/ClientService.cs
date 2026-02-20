
using ClientCore.Models;
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

                //if (createClientDTO.ContactIds != null && createClientDTO.ContactIds.Any())
                //{
                //    await LinkContactsToClientAsync(client.Id, createClientDTO.ContactIds);
                //}

                return client.Id;
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                throw new Exception("An error occurred while adding the client.", ex);
            }
        }

        public async Task<List<GetClientDTO>> GetAllAsync()
        {
            try
            {
                var clientDTOs = await _context.Clients
                    .Include(c => c.Contacts) 
                    .OrderBy(c => c.Name)
                    .Select(c => new GetClientDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Code = c.Code,
                        TotalContacts = c.Contacts != null ? c.Contacts.Count : 0
                    })
                    .ToListAsync();

                return clientDTOs;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving clients.", ex);
            }
        }

        public async Task<GetClientDTO> GetAsync(string id)
        {
            try
            {
                var client = await _context.Clients
                    .Select(c => new Client
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Code = c.Code
                    })
                    .FirstOrDefaultAsync(c => c.Id == id) ?? throw new Exception("Client not found");
                
                var clientDTO = _mapper.Map<GetClientDTO>(client);
                return clientDTO;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the client.", ex);
            }
        }

        public async Task LinkContactAsync(LinkContactDTO linkContactDTO)
        {
            if (linkContactDTO?.ClientId is null || linkContactDTO?.ContactId is null)
                throw new Exception("Client ID and Contact ID cannot be null");

            try
            {
                // Check if link already exists
                var exists = await _context.ClientContacts
                    .AnyAsync(cc => cc.ClientId == linkContactDTO.ClientId && cc.ContactId == linkContactDTO.ContactId);

                if (!exists)
                {
                    var clientContact = new ClientContact
                    {
                        Id = Guid.NewGuid().ToString(),
                        ClientId = linkContactDTO.ClientId,
                        ContactId = linkContactDTO.ContactId
                    };

                    await _context.ClientContacts.AddAsync(clientContact);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while linking contacts to client.", ex);
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

            StringBuilder alphaPart = new();

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

      
    }
}

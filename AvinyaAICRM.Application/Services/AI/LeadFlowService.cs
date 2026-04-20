using AvinyaAICRM.Application.DTOs.AI;
using AvinyaAICRM.Application.DTOs.Lead;
using AvinyaAICRM.Application.Interfaces.Clients;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Shared.AI;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.AI
{
    public class LeadFlowService : ILeadFlowService
    {
        private readonly IMemoryCache _cache;
        private readonly IClientRepository _clientRepo;
        private readonly ILeadService _leadService;
        private const string CachePrefix = "lead_flow_session:";

        public LeadFlowService(IMemoryCache cache, IClientRepository clientRepo, ILeadService leadService)
        {
            _cache = cache;
            _clientRepo = clientRepo;
            _leadService = leadService;
        }

        public async Task<AIResponse?> ProcessFlowAsync(string message, Guid tenantId, string userId)
        {
            var sessionKey = $"{CachePrefix}{tenantId}:{userId}";
            var input = message?.Trim() ?? "";

            if (!_cache.TryGetValue(sessionKey, out LeadFlowSession session))
            {
                var lower = input.ToLower();
                // Broader triggers for lead creation
                bool isTrigger = lower.Contains("lead") && 
                                (lower.Contains("create") || lower.Contains("add") || lower.Contains("new") || lower.Contains("capture"));

                if (isTrigger)
                {
                    session = new LeadFlowSession
                    {
                        CurrentStep = LeadFlowStep.CompanyName
                    };

                    // Try to extract company name if provided (e.g. "create lead for GTAVPVT")
                    var words = lower.Split(' ');
                    var forIndex = Array.FindLastIndex(words, w => w == "for" || w == "to" || w == "of");
                    if (forIndex != -1 && words.Length > forIndex + 1)
                    {
                        session.CompanyName = input.Split(' ').Last(); 
                        _cache.Set(sessionKey, session, TimeSpan.FromMinutes(10));
                        return await HandleCompanyNameAsync(session, sessionKey, tenantId);
                    }

                    _cache.Set(sessionKey, session, TimeSpan.FromMinutes(10));
                    return CreateStepResponse("Please provide the company name.");
                }
                return null;
            }

            session.LastInteraction = DateTime.Now;
            // var input = message.Trim(); // Removed this line as it is now at the top

            switch (session.CurrentStep)
            {
                case LeadFlowStep.CompanyName:
                    session.CompanyName = input;
                    return await HandleCompanyNameAsync(session, sessionKey, tenantId);

                case LeadFlowStep.MobileIdentification:
                    session.Mobile = input;
                    return await HandleMobileIdentificationAsync(session, sessionKey, tenantId);

                case LeadFlowStep.ContactPerson:
                    session.ContactPerson = input;
                    session.CurrentStep = LeadFlowStep.Mobile;
                    _cache.Set(sessionKey, session, TimeSpan.FromMinutes(10));
                    return CreateStepResponse("Please provide mobile number.");

                case LeadFlowStep.Mobile:
                    if (!IsValidMobile(input))
                        return CreateStepResponse("Please provide a valid mobile number (at least 10 digits).");

                    session.Mobile = input;
                    session.CurrentStep = LeadFlowStep.Requirement;
                    _cache.Set(sessionKey, session, TimeSpan.FromMinutes(10));
                    return CreateStepResponse("What are the requirement details for this lead?");

                case LeadFlowStep.Requirement:
                    if (string.IsNullOrEmpty(input))
                        return CreateStepResponse("Please specify the requirement details.");

                    session.Requirement = input;
                    session.CurrentStep = LeadFlowStep.Confirmation;
                    _cache.Set(sessionKey, session, TimeSpan.FromMinutes(10));
                    return CreateStepResponse($"I have all details:\nCompany: {session.CompanyName}\nPerson: {session.ContactPerson}\nMobile: {session.Mobile}\nReq: {session.Requirement}\n\nDo you want to create this lead? (Yes/No)");

                case LeadFlowStep.Confirmation:
                    if (input.ToLower().Contains("yes"))
                    {
                        // If we just identified an existing client, we MUST still ask for requirements
                        if (string.IsNullOrEmpty(session.Requirement))
                        {
                            session.CurrentStep = LeadFlowStep.Requirement;
                            _cache.Set(sessionKey, session, TimeSpan.FromMinutes(10));
                            return CreateStepResponse("What are the requirement details for this lead?");
                        }

                        var result = await CreateLeadAsync(session, userId);
                        _cache.Remove(sessionKey);
                        return result;
                    }
                    else if (input.ToLower().Contains("no"))
                    {
                        _cache.Remove(sessionKey);
                        return new AIResponse { Action = "message", SuccessMessage = "Lead creation process cancelled." };
                    }
                    else
                    {
                        return CreateStepResponse("Please reply with Yes to create the lead, or No to cancel.");
                    }

                default:
                    _cache.Remove(sessionKey);
                    return null;
            }
        }

        private async Task<AIResponse> HandleCompanyNameAsync(LeadFlowSession session, string key, Guid tenantId)
        {
            var clients = (await _clientRepo.FindByNameAsync(session.CompanyName, tenantId)).ToList();

            if (clients.Count == 0)
            {
                session.CurrentStep = LeadFlowStep.ContactPerson;
                _cache.Set(key, session, TimeSpan.FromMinutes(10));
                return CreateStepResponse("Please provide contact person name.");
            }
            else if (clients.Count == 1)
            {
                var client = clients[0];
                session.ExistingClientId = client.ClientID;
                session.CompanyName = client.CompanyName;
                session.ContactPerson = client.ContactPerson ?? "";
                session.Mobile = client.Mobile ?? "";
                session.CurrentStep = LeadFlowStep.Confirmation;
                _cache.Set(key, session, TimeSpan.FromMinutes(10));
                return CreateStepResponse($"I found an existing client: {session.CompanyName}. Should I create a lead for them? (Yes/No)");
            }
            else
            {
                session.CurrentStep = LeadFlowStep.MobileIdentification;
                _cache.Set(key, session, TimeSpan.FromMinutes(10));
                return CreateStepResponse("Multiple clients found with this company name. Please provide mobile number to identify the correct client.");
            }
        }

        private async Task<AIResponse> HandleMobileIdentificationAsync(LeadFlowSession session, string key, Guid tenantId)
        {
            var client = await _clientRepo.FindByNameAndMobileAsync(session.CompanyName, session.Mobile, tenantId);

            if (client != null)
            {
                session.ExistingClientId = client.ClientID;
                session.CompanyName = client.CompanyName;
                session.ContactPerson = client.ContactPerson ?? "";
                session.CurrentStep = LeadFlowStep.Confirmation;
                _cache.Set(key, session, TimeSpan.FromMinutes(10));
                return CreateStepResponse($"Client identified: {session.CompanyName}. Create lead? (Yes/No)");
            }
            else
            {
                session.CurrentStep = LeadFlowStep.ContactPerson;
                _cache.Set(key, session, TimeSpan.FromMinutes(10));
                return CreateStepResponse("Could not identify specific client with that mobile number. Let's create a NEW one. Please provide contact person name.");
            }
        }

        private async Task<AIResponse> CreateLeadAsync(LeadFlowSession session, string userId)
        {
            var dto = new LeadRequestDto
            {
                ClientID = session.ExistingClientId,
                CompanyName = session.CompanyName,
                ContactPerson = session.ContactPerson,
                Mobile = session.Mobile,
                RequirementDetails = session.Requirement,
                Date = DateTime.UtcNow,
                CreatedBy = userId,
                Notes = "Created via conversational flow. Req: " + session.Requirement
            };

            var result = await _leadService.CreateAsync(dto, userId);
            bool success = result.StatusCode == 200 || result.StatusCode == 201;

            return new AIResponse
            {
                Action = "message",
                SuccessMessage = success ? $"Lead successfully created for {session.CompanyName}." : "Error: " + result.StatusMessage,
                Intent = "create_lead",
                TotalTokens = 500
            };
        }

        private AIResponse CreateStepResponse(string text)
        {
            return new AIResponse
            {
                Action = "lead_creation_flow",
                IsClarificationRequired = true,
                ClarificationMessage = text,
                Intent = "create_lead",
                TotalTokens = 100
            };
        }

        private bool IsValidMobile(string mobile)
        {
            return !string.IsNullOrEmpty(mobile) && mobile.All(char.IsDigit) && mobile.Length >= 10;
        }
    }
}

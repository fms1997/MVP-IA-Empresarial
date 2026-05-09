using System.Security.Claims;
using LocalMind.Api.Data;
using LocalMind.Api.DTOs;
using LocalMind.Api.Models;
using LocalMind.Api.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IChatService _chatService;

    public ChatController(AppDbContext context, IChatService chatService)
    {
        _context = context;
        _chatService = chatService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(ChatRequest request)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        Conversation conversation;

        if (request.ConversationId is null)
        {
            conversation = new Conversation
            {
                UserId = userId,
                Title = request.Message.Length > 40
                    ? request.Message[..40]
                    : request.Message
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }
        else
        {
            conversation = await _context.Conversations
                .FirstAsync(x => x.Id == request.ConversationId && x.UserId == userId);
        }

        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Message
        };

        _context.ChatMessages.Add(userMessage);

        //var assistantResponse = await _chatService.GenerateResponseAsync(request.Message);
        var chatResult = await _chatService.GenerateResponseAsync(userId, request.Message);
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            //Content = assistantResponse
            Content = chatResult.Response
        };

        _context.ChatMessages.Add(assistantMessage);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            conversationId = conversation.Id,
            //response = assistantResponse
            response = chatResult.Response,
            usedRag = chatResult.UsedRag,
            chunksUsed = chatResult.ChunksUsed,
            sources = chatResult.Sources
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var conversations = await _context.Conversations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpGet("history/{conversationId}")]
    public async Task<IActionResult> GetConversation(int conversationId)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var conversation = await _context.Conversations
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == conversationId && x.UserId == userId);

        if (conversation is null)
            return NotFound();

        return Ok(new
        {
            conversation.Id,
            conversation.Title,
            Messages = conversation.Messages
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Role,
                    x.Content,
                    x.CreatedAt
                })
        });
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddGroup(Group group)
        {
            throw new System.NotImplementedException();
        }

        public void AddMessage(Message message)
        {
             _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public Task<DbLoggerCategory.Database.Connection> GetConnection(string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<Group> GetGroupForConnection(string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Message> GetMessage(int id)
        {
              return await _context.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public Task<Group> GetMessageGroup(string groupName)
        {
            throw new System.NotImplementedException();
        }      

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await _context.Messages
                    .Include(u => u.Sender).ThenInclude(p => p.Photos)
                    .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                    .Where(m => m.Recipient.UserName == currentUsername 
                          && m.RecipientDeleted == false
                          && m.Sender.UserName == recipientUsername
                          || m.Recipient.UserName == recipientUsername
                          && m.Sender.UserName == currentUsername
                          && m.SenderDeleted == false
                    )
                    .OrderBy(m => m.MessageSent)
                    .ToListAsync();

            var unreadMeassages = messages.Where(m => m.DateRead == null
                    && m.Recipient.UserName == currentUsername).ToList();
            
            if(unreadMeassages.Any()){
                foreach(var message in unreadMeassages){
                    message.DateRead = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }
            
            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public void RemoveConnection(DbLoggerCategory.Database.Connection connection)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
           var query = _context.Messages
           .OrderByDescending(m => m.MessageSent)
           .AsQueryable();

           query = messageParams.Container switch
           {
               "Inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username 
                    && u.RecipientDeleted == false),
               "Outbox" => query.Where(u => u.Sender.UserName == messageParams.Username 
                    && u.SenderDeleted == false),
               _ => query.Where(u => u.Recipient.UserName == 
                        messageParams.Username && u.RecipientDeleted == false && u.DateRead == null)            
           };

           var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

           return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }
    }
}
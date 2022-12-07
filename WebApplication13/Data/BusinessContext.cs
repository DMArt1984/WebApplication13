using FactPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Data
{
    public class BusinessContext:DbContext
    {
        public DbSet<myEvent> BEvents { get; set; } // События
        public DbSet<ServiceObject> ServiceObjects { get; set; } // Объекты обслуживания
        public DbSet<ObjectClaim> Claims { get; set; } // Свойства объектов
        public DbSet<Work> Works { get; set; } // Обслуживание объектов
        public DbSet<Alert> Alerts { get; set; } // Уведомления по объектам
        public DbSet<Step> Steps { get; set; } // Шаги обслуживания
        public DbSet<WorkStep> WorkSteps { get; set; } // Выполнение шагов обслуживания
        public DbSet<Level> Levels { get; set; } // Позиции объектов
        public DbSet<myFiles> Files { get; set; } // Пути к файлам
        public DbSet<myDictionary> Dic { get; set; } // Словарь
        

        private readonly IConfiguration Configuration;
        private readonly HttpContext _httpContext;
        private readonly string DefConnectionName = "apple";

        //private Dictionary<string, string> MyConnections = new Dictionary<string, string>();
        public BusinessContext(DbContextOptions<BusinessContext> options, IConfiguration configuration, IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContext = httpContextAccessor?.HttpContext;
            Configuration = configuration;

            //Database.EnsureCreated();   // создаем базу данных при первом обращении?
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string ConnectionString; // строка подключения
                try
                {
                    var ConnectionName_Header = _httpContext.Request.Headers["db"].ToString(); // имя в запросе
                    var ConnectionName_Cookie = _httpContext.Request.Cookies["company"]; // имя в браузере

                    if (String.IsNullOrEmpty(ConnectionName_Header)) // если в запросе нет
                        ConnectionName_Header = ConnectionName_Cookie; // берем из браузера

                    if (ConnectionName_Header == null) // если имени все еще нет, то
                        ConnectionName_Header = DefConnectionName; // берем его по умолчанию

                    ConnectionString = Configuration.GetConnectionString(ConnectionName_Header); 
                    if (ConnectionString == null) // если имя не найдено 
                        ConnectionString = Configuration.GetConnectionString(DefConnectionName); // берем имя по умолчанию

                } catch
                {
                    ConnectionString = Configuration.GetConnectionString(DefConnectionName); // берем имя по умолчанию
                }
                optionsBuilder.UseNpgsql(ConnectionString);
            }
        }

        


    }
}

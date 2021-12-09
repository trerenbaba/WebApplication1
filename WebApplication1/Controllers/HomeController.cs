using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            DatabaseContext db = new DatabaseContext();
            db.Books.ToList(); //Bu kod çalıştığı anda databasecontext tetikleniyor db oluşuyor.

            //Book book = new Book()
            //{
            //    Name = "Hayri Pıtırcık",
            //    Description = "Aleyna çok seviyormuş",
            //    PublishDate = DateTime.Now
            //};
            //db.Books.Add(book);
            //db.SaveChanges();
            db.ExecuteInsertFakeData();

            return View();
        }

    }

    public class DatabaseContext : DbContext
    {
        public DbSet<Book> Books { get; set; }

        public DatabaseContext()
        {
            Database.SetInitializer(new DbInitializer());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>() //storage procedure yapısını buralarda kuruyoruz.
                .MapToStoredProcedures(config =>
                {
                    config.Insert(i => i.HasName("BookInsertSp"));
                    config.Update(u =>
                    {
                        u.HasName("BookUpdateSP");
                        u.Parameter(p => p.Id, "bookId");
                    });
                    config.Delete(d => d.HasName("BookDeleteSP"));
                }); //Kayıt silme vs işlemleri dbSavechange denildiğinde burası çalışacak                                                                     

        }
        public void ExecuteInsertFakeData()
        {
            Database.ExecuteSqlCommand("EXEC InsertFakeDataSP");
        }

        //public void ExecuteGetBooksByPublishDateSP(int startyear, int endyear)
        //{
        //    Database.ExecuteSqlCommand("EXEC GetBooksByPublishDateSP @p0,@p1",startyear,endyear);
        //}

    }

    public class DbInitializer : CreateDatabaseIfNotExists<DatabaseContext>
    {
        protected override void Seed(DatabaseContext context)
        {
            //context.Database.ExecuteSqlCommand("Select * from Books Where Id=@p0 and Id=@p1", 5, 6);
            //context.Database.ExecuteSqlCommand("Select * from Books Where Id=@ilk and Id=@son",
            //   new SqlParameter("@ilk", 5),
            //   new SqlParameter("@son", 6));
            context.Database.ExecuteSqlCommand(
                @"
                create procedure InsertFakeDataSP
                as
                begin
                	insert into Books(Name,Description,PublishDate) values('Da Vinci Code','Da Vincinin Sifresi','2003-02-01')
                	insert into Books(Name,Description,PublishDate) values('Angels & Demons','Melekler ve Seytanlar','2003-03-30')
                	insert into Books(Name,Description,PublishDate) values('Lost Symbol','Kayip Sembol','2009-01-29')
                end ");
            context.Database.ExecuteSqlCommand(
                @"
                  create procedure GetBooksByPublishDateSP
                  @p0 datetime, --start date
                  @p1 datetime --enddate
                  as
                  begin
                  	select TBL.PublishDate,count(TBL.PublishDate) as [Count]
                  	from (
                  			select year(PublishDate) as PublishDate
                  			from Books
                  			where year(PublishDate) between @p0 and @p1
                  	) as TBL
                  	group by TBL.PublishDate
                    end
                    ");
            context.Database.ExecuteSqlCommand(
                @"
                 create view GetBooksInfoVW
                 as
                 select
                 Id,
                 Name+' : '+Description+' (' + CONVERT(nvarchar(20),PublishDate) + ')' as Info from Books ");
        }
    }

    //[Table("Books")]
    public class Book
    {
        public int Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? PublishDate { get; set; } //? nullable null gelebilir anlamına geliyor. Yazmazsak null kabul etmez.

    }
}
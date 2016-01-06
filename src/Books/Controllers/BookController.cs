using Microsoft.AspNet.Mvc;
using Books.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Entity;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Books.Repository;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Books.Controllers
{
    public class BookController : Controller
    {

        [FromServices]
        public GenericRepository<Book> Books { get; set; }

        [FromServices]
        public GenericRepository<Author> Authors { get; set; }

        [FromServices]
        public ILogger<BookController> Logger { get; set; }

        public IActionResult Index()
        {
            var books = Books.All().Include(_ => _.Author);
            return View(books);
        }

        public async Task<ActionResult> Details(int id)
        {
            var book = await Books.All()
                .Include(b => b.Author)
                .SingleOrDefaultAsync(b => b.BookID == id);
            if (book == null)
            {
                Logger.LogInformation("Details: Item not found {0}", id);
                return HttpNotFound();
            }
            return View(book);
        }

        public ActionResult Create()
        {
            ViewBag.Items = GetAuthorsListItems();
            return View();
        }

        private dynamic GetAuthorsListItems()
        {
            var tmp = Authors.All().ToList();

            // Create authors list for <select> dropdown Workaround for https://github.com/aspnet/EntityFramework/issues/2246
            return tmp
                .OrderBy(author => author.LastName)
                .Select(author => new SelectListItem
                {
                    Text = string.Format("{0}, {1}", author.LastName, author.FirstMidName),
                    Value = author.AuthorID.ToString()
                });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Title", "Year", "Price", "Genre", "AuthorID")] Book book)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Books.Add(book);
                    await Books.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save changes.");
            }
            return View(book);
        }

        public async Task<ActionResult> Edit(int id)
        {
            Book book = await FindBookAsync(id);
            if (book == null)
            {
                Logger.LogInformation("Edit: Item not found {0}", id);
                return HttpNotFound();
            }

            ViewBag.Items = GetAuthorsListItems(book.AuthorID);
            return View(book);
        }

        private dynamic GetAuthorsListItems(int authorID)
        {
            var tmp = Authors.All().ToList();  // Workaround for https://github.com/aspnet/EntityFramework/issues/2246

            // Create authors list for <select> dropdown
            return tmp
                .OrderBy(author => author.LastName)
                .Select(author => new SelectListItem
                {
                    Text = String.Format("{0}, {1}", author.LastName, author.FirstMidName),
                    Value = author.AuthorID.ToString(),
                    Selected = author.AuthorID == authorID
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Update(int id, [Bind("Title", "Year", "Price", "Genre", "AuthorID")] Book book)
        {
            try
            {
                Book bookhold = await FindBookAsync(id);
                bookhold.Title = book.Title;
                bookhold.AuthorID = book.AuthorID;
                bookhold.Price = book.Price;
                bookhold.Year = book.Year;
                bookhold.Genre = book.Genre;

                await Books.SaveChangesAsync();
      
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save changes.");
            }
            return View(book);
        }

        private Task<Book> FindBookAsync(int id)
        {
            return Books.All().SingleOrDefaultAsync(book => book.BookID == id);
        }


        [HttpGet]
        [ActionName("Delete")]
        public async Task<ActionResult> ConfirmDelete(int id, bool? retry)
        {
            Book book = await FindBookAsync(id);
            if (book == null)
            {
                Logger.LogInformation("Remove: Item not found {0}", id);
                return HttpNotFound();
            }
            ViewBag.Retry = retry ?? false;
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                Book book = await FindBookAsync(id);
                Books.Remove(book);
                await Books.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return RedirectToAction("Remove", new { id = id, retry = true });
            }
            return RedirectToAction("Index");
        }

    }
}

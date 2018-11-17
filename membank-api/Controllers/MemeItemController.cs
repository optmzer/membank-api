using System;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using membankApi.Models;
using membankApi.Helpers;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.Extensions.Configuration;
using static System.Net.Mime.MediaTypeNames;

namespace membankApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemeItemController : ControllerBase
    {
        private readonly membankApiContext _context;
        private IConfiguration _configuration;

        public MemeItemController(membankApiContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Meme/Tags
        [Route("tags")]
        [HttpGet]
        public async Task<List<string>> GetTags()
        {
            var memes = (from m in _context.MemeItemModel
                         select m.Tags).Distinct();

            var returned = await memes.ToListAsync();

            return returned;
        }

        [HttpPost, Route("upload")]
        public async Task<IActionResult> UploadFile([FromForm]MemeImageItemModel meme)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                using (var stream = meme.Image.OpenReadStream())
                {
                    var cloudBlock = await UploadToBlob(meme.Image.FileName, null, stream);
                    //// Retrieve the filename of the file you have uploaded
                    //var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
                    if (string.IsNullOrEmpty(cloudBlock.StorageUri.ToString()))
                    {
                        return BadRequest("An error has occured while uploading your file. Please try again.");
                    }

                    MemeItemModel memeItem = new MemeItemModel();
                    memeItem.Title = meme.Title;
                    memeItem.Tags = meme.Tags;

                    System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                    memeItem.Height = image.Height.ToString();
                    memeItem.Width = image.Width.ToString();
                    memeItem.Url = cloudBlock.SnapshotQualifiedUri.AbsoluteUri;
                    memeItem.Uploaded = DateTime.Now.ToString();

                    _context.MemeItemModel.Add(memeItem);
                    await _context.SaveChangesAsync();

                    return Ok($"File: {meme.Title} has successfully uploaded");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error has occured. Details: {ex.Message}");
            }
        }

        private async Task<CloudBlockBlob> UploadToBlob(string filename, byte[] imageBuffer = null, System.IO.Stream stream = null)
        {
            var accountName = _configuration["AzureBlob:name"];
            var accountKey = _configuration["AzureBlob:key"]; ;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer imagesContainer = blobClient.GetContainerReference("images");

            string storageConnectionString = _configuration["AzureBlob:connectionString"];

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Generate a new filename for every new blob
                    var fileName = Guid.NewGuid().ToString();
                    fileName += GetFileExtention(filename);

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = imagesContainer.GetBlockBlobReference(fileName);

                    if (stream != null)
                    {
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                    else
                    {
                        return new CloudBlockBlob(new Uri(""));
                    }

                    return cloudBlockBlob;
                }
                catch (StorageException ex)
                {
                    return new CloudBlockBlob(new Uri(""));
                }
            }
            else
            {
                return new CloudBlockBlob(new Uri(""));
            }
        }

        private string GetFileExtention(string fileName)
        {
            if (!fileName.Contains("."))
                return ""; //no extension
            else
            {
                var extentionList = fileName.Split('.');
                return "." + extentionList.Last(); //assumes last item is the extension 
            }
        }

        // GET: api/MemeItemModels
        [HttpGet]
        public IEnumerable<MemeItemModel> GetMemeItemModel()
        {
            return _context.MemeItemModel;
        }

        // GET: api/MemeItemModels/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMemeItemModel([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var memeItemModel = await _context.MemeItemModel.FindAsync(id);

            if (memeItemModel == null)
            {
                return NotFound();
            }

            return Ok(memeItemModel);
        }

        // PUT: api/MemeItemModels/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMemeItemModel([FromRoute] int id, [FromBody] MemeItemModel memeItemModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != memeItemModel.Id)
            {
                return BadRequest();
            }

            _context.Entry(memeItemModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MemeItemModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/MemeItemModels
        [HttpPost]
        public async Task<IActionResult> PostMemeItemModel([FromBody] MemeItemModel memeItemModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.MemeItemModel.Add(memeItemModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMemeItemModel", new { id = memeItemModel.Id }, memeItemModel);
        }

        // DELETE: api/MemeItemModels/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMemeItemModel([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var memeItemModel = await _context.MemeItemModel.FindAsync(id);
            if (memeItemModel == null)
            {
                return NotFound();
            }

            _context.MemeItemModel.Remove(memeItemModel);
            await _context.SaveChangesAsync();

            return Ok(memeItemModel);
        }

        private bool MemeItemModelExists(int id)
        {
            return _context.MemeItemModel.Any(e => e.Id == id);
        }
    }
}
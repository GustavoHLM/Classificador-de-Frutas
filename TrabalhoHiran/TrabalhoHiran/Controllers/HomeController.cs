using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using TrabalhoHiran.Models;
using static System.Net.WebRequestMethods;

namespace TrabalhoHiran.Controllers
{
    public class HomeController : Controller
    {
        string endpointUrl = "";
        string primaryKey = "";
        string databaseId = "bancoimg";
        string containerId = "imagem";

        private readonly ILogger<HomeController> _logger;

        public static byte[] _Imagem { get; set; }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ImageModel imageModel = new ImageModel();
            List<ImagemContext> aux = await GetImagesFromCosmosDB();
            foreach (var item in aux)
            {
                imageModel.ImageFile.Add(item.img);
                imageModel.classificacao.Add(item.classificacao ?? "");
                if(item.classificacao == "errado")
                {
                    imageModel.erros += 1;
                }
            }

            List<ClasseContext> aux2 = await GetClassesCosmosDB();

            foreach (var item in aux2)
            {
                imageModel.classeContext.Add(item);
            }
            return View(imageModel);
        }

        public async Task UpsertClasseContext(ImageModel model)
        {

            using (var client = new CosmosClient(endpointUrl, primaryKey))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer("classe");
                var id = model.classificacaoAux[0].ToString();
                ClasseContext imageDocument;
                try
                {
                    var response = await container.ReadItemAsync<ClasseContext>(id, new PartitionKey(id));
                    imageDocument = response.Resource;
                    imageDocument.acertos += 1;

                    var replaceResponse = await container.ReplaceItemAsync(imageDocument, id, new PartitionKey(id));
                }
                catch (CosmosException ex)
                {
                    imageDocument = new ClasseContext
                    {
                        id = id,
                        classe = model.classificacaoAux.Substring(2),
                        acertos = 1
                    };
                    var upsertResponse = await container.CreateItemAsync(imageDocument);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> confirmado(ImageModel model)
        {
            using (var client = new CosmosClient(endpointUrl, primaryKey))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);
                ImagemContext imageDocument = new ImagemContext
                {
                    id = Guid.NewGuid().ToString(), // Você pode usar um ID único
                    img = _Imagem,
                    classificacao = model.classificacaoAux,
                };
                var response = await container.CreateItemAsync(imageDocument);
                await UpsertClasseContext(model);
            }
            return RedirectToAction("Index");
    }

        public async Task<IActionResult> Cancelar()
        {
            using (var client = new CosmosClient(endpointUrl, primaryKey))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);
                ImagemContext imageDocument = new ImagemContext
                {
                    id = Guid.NewGuid().ToString(),
                    img = _Imagem,
                    classificacao = "errado",
                };
                var response = await container.CreateItemAsync(imageDocument); 
            }
            return RedirectToAction("Index");    
        }

    public async Task<List<ImagemContext>> GetImagesFromCosmosDB()
        {
            using (var client = new CosmosClient(endpointUrl, primaryKey))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);

                var query = container.GetItemQueryIterator<ImagemContext>(new QueryDefinition("SELECT * FROM c"));
                var images = new List<ImagemContext>();


                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    images.AddRange(response);
                }
                return images;

            }
        }

        public async Task<List<ClasseContext>> GetClassesCosmosDB()
        {
            using (var client = new CosmosClient(endpointUrl, primaryKey))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer("classe");

                var query = container.GetItemQueryIterator<ClasseContext>(new QueryDefinition("SELECT * FROM c"));
                var images = new List<ClasseContext>();


                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    images.AddRange(response);
                }
                return images;

            }
        }


        [HttpPost]
        public async Task<IActionResult> UploadImage(ImageModel model)
        {
            if (model.ImageUp != null && model.ImageUp.Length > 0)
            {
                using (var httpClient = new HttpClient())
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StreamContent(model.ImageUp.OpenReadStream()), "file", model.ImageUp.FileName);

                    var x = await httpClient.PostAsync("http://127.0.0.1:5000/", content);


                    if (x.IsSuccessStatusCode)
                    {
                        string responseContent = await x.Content.ReadAsStringAsync();
                        var jsonResponse = JObject.Parse(responseContent);

                        model.classificacaoAux = jsonResponse["classification"].Value<string>();
                    }
                }
                using (var stream = model.ImageUp.OpenReadStream())
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        var imageBytes = reader.ReadBytes((int)stream.Length);
                        model.ImageData = imageBytes;
                        _Imagem = imageBytes;
                    }
                }  
            }
            return View("Confirmacao", model);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
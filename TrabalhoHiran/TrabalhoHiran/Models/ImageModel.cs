using System.Security.Cryptography;

namespace TrabalhoHiran.Models
{
    public class ImageModel
    {
        public List<byte[]> ImageFile { get; set; } = new List<byte[]>();
        public IFormFile ImageUp { get; set; }
        public byte[] ImageData { get; set; }
        public List<string> classificacao { get; set; } = new List<string>();
        public string classificacaoAux { get; set; } 
        public List<ClasseContext> classeContext { get; set; } = new List<ClasseContext>();

        public int erros {  get; set; } 
    }

}

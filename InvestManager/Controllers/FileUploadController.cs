using Microsoft.AspNetCore.Mvc;
using iText;
using System;
using iText.Kernel.Pdf;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Data.OleDb;

namespace InvestManager.Controllers
{
    public class FileUploadController : Controller
    {
        //Define uma instância de IHostingEnvironment
        IHostingEnvironment _appEnvironment;
        //Injeta a instância no construtor para poder usar os recursos
        public FileUploadController(IHostingEnvironment env)
        {
            _appEnvironment = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        //método para enviar os arquivos usando a interface IFormFile
        [HttpPost("FileUpload")]
        public async Task<IActionResult> Index(List<IFormFile> archives)
        {
            long tamanhoArquivos = archives.Sum(f => f.Length);
            // caminho completo do arquivo na localização temporária
            var caminhoArquivo = Path.GetTempFileName();

            // processa os arquivo enviados
            //percorre a lista de arquivos selecionados
            foreach (var arquivo in archives)
            {
                //verifica se existem arquivos 
                if (arquivo == null || arquivo.Length == 0)
                {
                    //retorna a viewdata com erro
                    ViewData["Erro"] = "Error: Arquivo(s) não selecionado(s)";
                    return View(ViewData);
                }

                using (OleDbConnection connection = new OleDbConnection("inserir excel aqui"))
                {
                    connection.Open();
                    OleDbCommand command = new OleDbCommand("select * from [Sheet1$]", connection);
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var row1Col0 = dr[0];
                            Console.WriteLine(row1Col0);
                        }
                    }
                }

            }
            //monta a ViewData que será exibida na view como resultado do envio 
            ViewData["Resultado"] = $"{archives.Count} arquivos foram enviados ao servidor, " +
             $"com tamanho total de : {tamanhoArquivos} bytes";

            //retorna a viewdata
            return View(ViewData);
        }
    }
}

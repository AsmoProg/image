using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Spectre.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Créer un menu Spectre.Console
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Que souhaitez-vous faire ?")
                .PageSize(10)
                .AddChoices(new[] { "Télécharger les images", "Quitter" })
        );

        if (choice == "Télécharger les images")
        {
            await DownloadImagesFromCategories();
        }
    }

    static async Task DownloadImagesFromCategories()
    {
        int categoryNumber = 45114;  // Le numéro de la première catégorie
        string baseDirectory = "images";  // Dossier de sauvegarde principal
        bool categoryExists = true;  // Variable pour contrôler si une catégorie contient des images

        using (HttpClient client = new HttpClient())
        {
            while (categoryExists)
            {
                // Créer un dossier pour la catégorie actuelle
                string categoryDirectory = System.IO.Path.Combine(baseDirectory, categoryNumber.ToString());
                if (!System.IO.Directory.Exists(categoryDirectory))
                {
                    System.IO.Directory.CreateDirectory(categoryDirectory);
                }

                AnsiConsole.MarkupLine($"[yellow]Téléchargement des images pour la catégorie {categoryNumber} dans le dossier {categoryDirectory}...[/]");

                bool imagesFound = false;  // Pour vérifier si des images ont été trouvées dans cette catégorie

                // Boucle sur les pages de la catégorie jusqu'à ce qu'il n'y ait plus d'images
                for (int pageNumber = 1; ; pageNumber++)
                {
                    string url = $"https://www.scan-trad.com/scan/{categoryNumber}/{pageNumber}";

                    string pageHtml = await client.GetStringAsync(url);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(pageHtml);

                    // Extraire l'URL de l'image dans la classe "col text-center book-page"
                    var imageNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'col text-center book-page')]//img");

                    if (imageNode != null)
                    {
                        string imageUrl = imageNode.GetAttributeValue("src", string.Empty);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // Si l'URL de l'image est relative, convertir en URL absolue
                            if (!imageUrl.StartsWith("http"))
                            {
                                Uri baseUri = new Uri(url);
                                Uri fullUri = new Uri(baseUri, imageUrl);
                                imageUrl = fullUri.AbsoluteUri;
                            }

                            // Télécharger l'image
                            var imageData = await client.GetByteArrayAsync(imageUrl);
                            string imagePath = System.IO.Path.Combine(categoryDirectory, $"image_page_{pageNumber}.jpg");

                            // Sauvegarder l'image
                            System.IO.File.WriteAllBytes(imagePath, imageData);
                            AnsiConsole.MarkupLine($"[green]Image téléchargée avec succès : {imagePath}[/]");
                            imagesFound = true;  // Une image a été trouvée sur cette page
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Aucune image trouvée sur cette page.[/]");
                            break;  // Si aucune image valide, on arrête de chercher dans cette catégorie
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Pas d'image trouvée sur cette page.[/]");
                        break;  // Si aucune image trouvée, on arrête de chercher dans cette catégorie
                    }
                }

                // Si aucune image n'a été trouvée pour cette catégorie, on incrémente la catégorie
                if (!imagesFound)
                {
                    AnsiConsole.MarkupLine($"[yellow]Aucune image trouvée pour la catégorie {categoryNumber}, passage à la suivante...[/]");
                    categoryNumber++;
                }
                else
                {
                    // Si des images ont été trouvées, continuer avec la même catégorie
                    AnsiConsole.MarkupLine($"[green]Images trouvées pour la catégorie {categoryNumber}. Passons à la catégorie suivante...[/]");
                    categoryNumber++;
                }

                // Vérifier si la catégorie suivante existe en tentant de récupérer la première image
                if (!await CategoryHasImages(client, categoryNumber))
                {
                    AnsiConsole.MarkupLine($"[red]Aucune image trouvée dans la catégorie {categoryNumber}, arrêt du processus...[/]");
                    categoryExists = false;
                }
            }
        }
    }

    static async Task<bool> CategoryHasImages(HttpClient client, int categoryNumber)
    {
        // Tenter de charger la première page de la catégorie pour vérifier si elle contient des images
        string url = $"https://www.scan-trad.com/scan/{categoryNumber}/1";
        string pageHtml = await client.GetStringAsync(url);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(pageHtml);

        // Vérifier si la page contient une image dans la classe "col text-center book-page"
        var imageNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'col text-center book-page')]//img");
        return imageNode != null;  // Si une image est trouvée, la catégorie existe
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Custom;
using Azure.AI.FormRecognizer.Models;
using static Azure.AI.FormRecognizer.FormRecognizerClientOptions;

namespace forms_sdk_test
{
    class Program
    {
        const string EnvEndpoint = "FR_ENDPOINT";
        const string EnvApiKey = "FR_KEY";
        static async Task Main(string[] args)
        {
            var endpoint = Environment.GetEnvironmentVariable(EnvEndpoint) ?? throw new ArgumentNullException(EnvEndpoint);
            var key = Environment.GetEnvironmentVariable(EnvApiKey) ?? throw new ArgumentNullException(EnvApiKey);

            var frEndpoint = new Uri(endpoint);
            var frKey = new FormRecognizerApiKeyCredential(key);
            var frOptions = new FormRecognizerClientOptions(ServiceVersion.V2_0_Preview);

            var custom = new CustomFormClient(frEndpoint, frKey, frOptions);
            var receipts = new ReceiptClient(frEndpoint, frKey, frOptions);
            var layout = new FormLayoutClient(frEndpoint, frKey, frOptions);

            //   await TrainCustomModelWithLabels(custom);
            //   await AnalyzeWithCustomModelWithLabels(custom);

            await Task.CompletedTask;
        }

        static async Task GetCustomModelPage(CustomFormClient client)
        {
            var pages = client.GetModelsAsync().AsPages().GetAsyncEnumerator();
            await pages.MoveNextAsync();
            Console.WriteLine(pages.Current.ContinuationToken);
            Console.WriteLine(pages.Current.Values.Count);
        }

        static async Task TrainCustomModel(CustomFormClient client)
        {
            var source = "https://chstoneforms.blob.core.windows.net/samples?st=2020-03-09T01%3A13%3A09Z&se=2030-03-09T20%3A13%3A00Z&sp=rl&sv=2018-03-28&sr=c&sig=F4IO9V78SHJwdlURXD5%2Bdvk%2Bnv9OBqT0Sdu6xJgCtUk%3D";
            var poller = await client.StartTrainingAsync(source);
            Console.WriteLine($"Waiting for model {poller.Id}...");
            var resp = await poller.WaitForCompletionAsync(TimeSpan.FromSeconds(1));
            Console.WriteLine($"{resp.Value.ModelId} | {resp.Value.TrainingStatus.CreatedOn} | {resp.Value.TrainingStatus.LastUpdatedOn} | {resp.Value.TrainingStatus.ModelId} | {resp.Value.TrainingStatus.TrainingStatus}");
            foreach (var form in resp.Value.LearnedForms)
            {
                Console.WriteLine($"FormTypeId: {form.FormTypeId}");
                foreach (var field in form.LearnedFields)
                {
                    Console.WriteLine($"- {field}");
                }
            }
            foreach (var info in resp.Value.TrainingInfo.PerDocumentInfo)
            {
                Console.WriteLine($"{info.DocumentName} | {info.Pages} | {info.Status}");
                foreach (var error in info.Errors)
                {
                    Console.WriteLine($"{error.Code} | {error.Message}");
                }
            }
        }

        static async Task TrainCustomModelWithLabels(CustomFormClient client)
        {
            var source = "https://chstoneforms.blob.core.windows.net/samples2?st=2020-03-09T09%3A59%3A50Z&se=2030-03-10T03%3A59%3A00Z&sp=rl&sv=2018-03-28&sr=c&sig=PCqzIFWNvRMRcbAlMIGeLJabElSZAelS21p8Cc48exs%3D";
            var poller = await client.StartTrainingWithLabelsAsync(source);
            Console.WriteLine($"Waiting for model {poller.Id}...");
            var resp = await poller.WaitForCompletionAsync(TimeSpan.FromSeconds(1));
            Console.WriteLine($"{resp.Value.ModelId} | {resp.Value.TrainingStatus.CreatedOn} | {resp.Value.TrainingStatus.LastUpdatedOn} | {resp.Value.TrainingStatus.ModelId} | {resp.Value.TrainingStatus.TrainingStatus} | {resp.Value.AverageLabelAccuracy}");
            foreach (var field in resp.Value.LabelAccuracies)
            {
                Console.WriteLine($"- {field.Label} - {field.Accuracy}");
            }
            foreach (var info in resp.Value.TrainingInfo.PerDocumentInfo)
            {
                Console.WriteLine($"- {info.DocumentName} | {info.Pages} | {info.Status}");
                foreach (var error in info.Errors)
                {
                    Console.WriteLine($"{error.Code} | {error.Message}");
                }
            }
        }

        static async Task AnalyzeWithCustomModel(CustomFormClient client)
        {
            var modelId = "a7f013f1-13f6-4594-b5a1-1e8b9e093520";
            var filePath = @"C:\Users\ctsto\Downloads\sample_data\Test\Invoice_6.pdf";
            using var stream = File.OpenRead(filePath);
            var poller = await client.StartExtractFormAsync(modelId, stream, FormContentType.Pdf, true);
            await Analyze(poller);
        }

        static async Task AnalyzeWithCustomModelWithLabels(CustomFormClient client)
        {
            var modelId = "91c8da74-3bde-44e5-9fd4-3d8ae150c0bf";
            var filePath = @"C:\Users\ctsto\Downloads\sample_data\Test\Invoice_6.pdf";
            using var stream = File.OpenRead(filePath);
            var poller = await client.StartExtractFormAsync(modelId, stream, FormContentType.Pdf, true);
            await Analyze(poller);
        }

        static async Task Analyze(ExtractFormOperation poller)
        {
            Console.WriteLine($"Waiting for result {poller.Id}...");
            var result = await poller.WaitForCompletionAsync(TimeSpan.FromSeconds(1));
            Console.WriteLine($"Page Range: {result.Value.PageRange.StartPageNumber} - {result.Value.PageRange.EndPageNumber}");
            Console.WriteLine($"Type: {result.Value.LearnedFormType}");
            Console.WriteLine($"Pages: {result.Value.Pages.Count}");
            foreach (var page in result.Value.Pages)
            {
                Console.WriteLine($"- Page #: {page.PageNumber}");
                foreach (var table in page.Tables)
                {
                    Console.WriteLine("  - Table");
                    foreach (var cell in table.Cells)
                    {
                        Console.WriteLine($"     - text={cell.Text}, row={cell.RowIndex}, rowSpan={cell.RowSpan}, col={cell.ColumnIndex}, colSpan={cell.ColumnSpan}, confidence={cell.Confidence}, head={cell.IsHeader}, foot={cell.IsFooter}, #elements={cell.RawExtractedItems?.Count}");
                    }
                }

                Console.WriteLine("Fields");
                foreach (var field in page.Fields)
                {
                    Console.WriteLine($"- {field.Label} | {field.LabelOutline?.Points?.Length} | {field.LabelRawExtractedItems?.Count} | {field.Value} | {field.ValueOutline?.Points?.Length} | {field.ValueRawExtractedItems.Count} | {field.Confidence}");
                }
                Console.WriteLine($"Raw: {page.RawExtractedPage?.Width} {page.RawExtractedPage?.Unit} | {page.RawExtractedPage?.Height} {page.RawExtractedPage?.Unit} | {page.RawExtractedPage?.Angle} | {page.RawExtractedPage?.Language} | {page.RawExtractedPage?.Page} | {page.RawExtractedPage?.Lines?.Count}");
            }
        }

        static async Task ListCustomModels(CustomFormClient client)
        {
            var models = client.GetModelsAsync();
            await foreach (var model in models)
            {
                Console.WriteLine($"{model.ModelId} | {model.TrainingStatus} | {model.CreatedOn} | {model.LastUpdatedOn}");
            }
        }
    }
}

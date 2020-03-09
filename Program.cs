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

            // await TrainCustomModelWithLabels(custom);
            // await AnalyzeWithCustomModelWithLabels(custom);
            // await AnalyzeLayout(layout);
            // await AnalyzeReceipt(receipts);
            // await GetModelSummary(custom);
            // await DeleteModel(custom);

            await Task.CompletedTask;
        }

        // static async Task GetModel(CustomFormClient client)
        // {
        //     var modelId = "91c8da74-3bde-44e5-9fd4-3d8ae150c0bf";
        //     var resp = client.StartTrainingAsync()
        // }

        static async Task DeleteModel(CustomFormClient client)
        {
            var modelId = "51ee990c-bb72-49c5-a31f-3a7f62bfc40b";
            var resp = await client.DeleteModelAsync(modelId);
            Console.WriteLine("Deleted.");
        }

        static async Task GetModelSummary(CustomFormClient client)
        {
            var resp = await client.GetModelSubscriptionPropertiesAsync();
            var result = resp.Value;
            Console.WriteLine($"Count: {result.Count}");
            Console.WriteLine($"Limit: {result.Limit}");
            Console.WriteLine($"LastUpdated: {result.LastUpdatedOn}");
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

        static async Task AnalyzeLayout(FormLayoutClient client)
        {
            var filePath = "/Users/chstone/Downloads/sample_data/Test/Invoice_6.pdf";
            using var stream = File.OpenRead(filePath);
            var poller = await client.StartExtractLayoutAsync(stream, FormContentType.Pdf, true);
            var resp = await poller.WaitForCompletionAsync(TimeSpan.FromSeconds(1));
            var result = resp.Value;
            foreach (var page in result)
            {
                Console.WriteLine($"- Page#: {page.PageNumber}");
                Console.WriteLine($"  Angle: {page.RawExtractedPage.Angle}");
                Console.WriteLine($"  Width: {page.RawExtractedPage.Width} {page.RawExtractedPage.Unit}");
                Console.WriteLine($"  Height: {page.RawExtractedPage.Height} {page.RawExtractedPage.Unit}");
                Console.WriteLine($"  Lang: {page.RawExtractedPage.Language}");
                Console.WriteLine($"  Page#: {page.RawExtractedPage.Page}");
                Console.WriteLine($"  LineCount: {page.RawExtractedPage.Lines.Count}");
                Console.WriteLine($"  Tables:");
                foreach (var table in page.Tables)
                {
                    Console.WriteLine($"  - Dims: {table.ColumnCount} x {table.RowCount}");
                    Console.WriteLine($"    Cells:");
                    foreach (var cell in table.Cells)
                    {
                        Console.WriteLine($"    - Text: {cell.Text}");
                        Console.WriteLine($"      Col: {cell.ColumnIndex}");
                        Console.WriteLine($"      Row: {cell.RowIndex}");
                        Console.WriteLine($"      ColSpan: {cell.ColumnSpan}");
                        Console.WriteLine($"      RowSpan: {cell.RowSpan}");
                        Console.WriteLine($"      Confidence: {cell.Confidence}");
                        Console.WriteLine($"      Head: {cell.IsHeader}");
                        Console.WriteLine($"      Foot: {cell.IsFooter}");
                        Console.WriteLine($"      Box: {GetBoundingBoxString(cell.BoundingBox)}");
                    }
                }
            }
        }

        static async Task AnalyzeReceipt(ReceiptClient client)
        {
            var filePath = "/Users/chstone/Downloads/contoso-allinone.jpg";
            using var stream = File.OpenRead(filePath);
            var resp = await client.ExtractReceiptAsync(stream, FormContentType.Jpeg, true);
            var result = resp.Value;
            Console.WriteLine($"Receipt:");
            Console.WriteLine($"  Start: {result.PageRange.StartPageNumber}");
            Console.WriteLine($"  End: {result.PageRange.EndPageNumber}");
            Console.WriteLine($"  Type: {result.ReceiptType}");
            Console.WriteLine($"  MerchantName: {result.MerchantName}");
            Console.WriteLine($"  MerchantAddress: {result.MerchantAddress}");
            Console.WriteLine($"  MerchantPhoneNumber: {result.MerchantPhoneNumber}");
            Console.WriteLine($"  TransactionDate: {result.TransactionDate}");
            Console.WriteLine($"  TransactionTime: {result.TransactionTime}");
            Console.WriteLine($"  TransactionDate2: {result.ExtractedFields["TransactionDate"].Text}");
            Console.WriteLine($"  Subtotal: {result.Subtotal}");
            Console.WriteLine($"  Tax: {result.Tax}");
            Console.WriteLine($"  Tip: {result.Tip}");
            Console.WriteLine($"  Total: {result.Total}");
            Console.WriteLine($"  Items:");
            foreach (var item in result.Items)
            {
                Console.WriteLine($"  - Name: {item.Name}");
                Console.WriteLine($"  - Quantity: {item.Quantity}");
                // Console.WriteLine($"  - Price: {item.Price}");
                Console.WriteLine($"  - TotalPrice: {item.TotalPrice}");
            }
            Console.WriteLine($"  Page:");
            Console.WriteLine($"    Number: {result.RawExtractedPage.Page}");
            Console.WriteLine($"    Width: {result.RawExtractedPage.Width} {result.RawExtractedPage.Unit}");
            Console.WriteLine($"    Height: {result.RawExtractedPage.Height} {result.RawExtractedPage.Unit}");
            Console.WriteLine($"    Angle: {result.RawExtractedPage.Angle}");
            Console.WriteLine($"    Language: {result.RawExtractedPage.Language}");
            Console.WriteLine($"    LineCount: {result.RawExtractedPage.Lines.Count}");
        }

        static string GetBoundingBoxString(BoundingBox box)
        {
            return string.Join(", ", box.Points
                .Select((p) => $"({p.X}, {p.Y})"));
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

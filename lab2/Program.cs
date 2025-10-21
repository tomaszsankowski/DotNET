using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

ExcelPackage.License.SetNonCommercialOrganization("Politechnika Gdańska");

var file = new FileInfo(@"labEpp.xlsx");
file.Delete();

int row = 2;
var allFiles = new List<FileInfo>();

using (ExcelPackage ep = new(file))
{
    // --- Variables ---
    string path = "C:\\Users\\sanko\\Desktop\\jpnet\\lab2";
    int depth = 4;

    int outlineLevel = 0;

    // --- Setup workbook ---
    ep.Workbook.Properties.Title = "Lab2";
    ep.Workbook.Properties.Author = "s193363";
    ep.Workbook.Properties.Company = "PG";

    // --- Setup directory structure worksheet ---
    ExcelWorksheet strukturaKataloguWs = ep.Workbook.Worksheets.Add("Struktura katalogu");

    strukturaKataloguWs.Cells[1, 1].Value = "Path";
    strukturaKataloguWs.Cells[1, 2].Value = "Extension";
    strukturaKataloguWs.Cells[1, 3].Value = "Size";
    strukturaKataloguWs.Cells[1, 4].Value = "Attributes";

    // --- List files recursively ---
    if (File.Exists(path))
    {
        // This path is a file
        ProcessFile(path, strukturaKataloguWs, outlineLevel);
    }
    else if (Directory.Exists(path))
    {
        // This path is a directory
        ProcessDirectory(path, depth, strukturaKataloguWs, outlineLevel);
    }
    else
    {
        Console.WriteLine("{0} is not a valid file or directory.", path);
    }

    strukturaKataloguWs.Cells.AutoFitColumns(0);

    // --- Setup statistics worksheet ---
    ExcelWorksheet statystykiWs = ep.Workbook.Worksheets.Add("Statystyki");

    statystykiWs.Cells[1, 1].Value = "Path";
    statystykiWs.Cells[1, 2].Value = "Extension";
    statystykiWs.Cells[1, 3].Value = "Size";

    // --- Top 10 largest files ---
    var top10Files = allFiles.OrderByDescending(f => f.Length).Take(10).ToList();

    row = 2;
    foreach (var f in top10Files)
    {
        statystykiWs.Cells[row, 1].Value = f.FullName;
        statystykiWs.Cells[row, 2].Value = f.Extension;
        statystykiWs.Cells[row, 3].Value = f.Length;
        row++;
    }

    if (allFiles.Count == 0)
    {
        ep.Save();
        return;
    }

    // --- Extensions statistics ---
    var byExt = allFiles.GroupBy(f => string.IsNullOrEmpty(f.Extension) ? "no extension" : f.Extension.ToLower()).Select(g => new
        {
            Ext = g.Key,
            Count = g.Count(),
            Size = g.Sum(f => f.Length)
        }).OrderByDescending(g => g.Size).ToList();

    int startRow = row + 2;
    statystykiWs.Cells[startRow, 1].Value = "Extension";
    statystykiWs.Cells[startRow, 2].Value = "Count";
    statystykiWs.Cells[startRow, 3].Value = "TotalSize";

    int dataRow = startRow + 1;
    foreach (var e in byExt)
    {
        statystykiWs.Cells[dataRow, 1].Value = e.Ext;
        statystykiWs.Cells[dataRow, 2].Value = e.Count;
        statystykiWs.Cells[dataRow, 3].Value = e.Size;
        dataRow++;
    }

    // --- Circle plots ---
    var chartCount = (statystykiWs.Drawings.AddChart("PlotCount", eChartType.Pie3D) as ExcelPieChart);

    chartCount.Title.Text = "Udział ilościowy plików wg rozszerzenia";
    chartCount.SetPosition(1, 0, 4, 0);
    chartCount.SetSize(600, 300);

    var valAdd = new ExcelAddress(startRow + 1, 2, dataRow - 1, 2);
    var catAdd = new ExcelAddress(startRow + 1, 1, dataRow - 1, 1);

    var ser = chartCount.Series.Add(valAdd.Address, catAdd.Address) as ExcelPieChartSerie;

    ser.DataLabel.ShowCategory = true;
    ser.DataLabel.ShowPercent = true;

    var chartSize = (statystykiWs.Drawings.AddChart("PlotSize", eChartType.Pie3D) as ExcelPieChart);

    chartSize.Title.Text = "Udział rozmiarowy plików wg rozszerzenia";
    chartSize.SetPosition(20, 0, 4, 0);
    chartSize.SetSize(600, 300);

    var valAdd2 = new ExcelAddress(startRow + 1, 3, dataRow - 1, 3);
    var catAdd2 = new ExcelAddress(startRow + 1, 1, dataRow - 1, 1);

    var ser2 = chartSize.Series.Add(valAdd2.Address, catAdd2.Address) as ExcelPieChartSerie;

    ser2.DataLabel.ShowCategory = true;
    ser2.DataLabel.ShowPercent = true;

    statystykiWs.Cells.AutoFitColumns(0);

    // --- Save workbook ---
    ep.Save();
}
void ProcessDirectory(string targetDirectory, int depth, ExcelWorksheet ws, int outlineLevel)
{
    try
    {
        ws.Cells[row, 1].Value = targetDirectory;
        ws.Cells[row, 2].Value = "<DIR>";
        ws.Cells[row, 3].Value = "";
        ws.Cells[row, 4].Value = (File.GetAttributes(targetDirectory) & FileAttributes.Hidden) == FileAttributes.Hidden
            ? "Hidden"
            : "";

        ws.Row(row).OutlineLevel = outlineLevel;

        row++;

        if (depth <= 0)
        {
            return;
        }

        // Process the list of files found in the directory.
        string[] fileEntries = Directory.GetFiles(targetDirectory);
        foreach (string fileName in fileEntries)
            ProcessFile(fileName, ws, outlineLevel + 1);

        // Recurse into subdirectories of this directory.
        string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
        foreach (string subdirectory in subdirectoryEntries)
            ProcessDirectory(subdirectory, depth - 1, ws, outlineLevel + 1);

        ws.Row(row).OutlineLevel = outlineLevel;
        row++;
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine($"Brak dostępu do: {targetDirectory}");
    }
    catch (IOException)
    {
        Console.WriteLine($"Błąd wejścia/wyjścia w katalogu: {targetDirectory}");
    }
}

// Insert logic for processing found files here.
void ProcessFile(string path, ExcelWorksheet ws, int outlineLevel)
{
    try
    {
        var fileInfo = new FileInfo(path);
        ws.Cells[row, 1].Value = path;
        ws.Cells[row, 2].Value = fileInfo.Extension;
        ws.Cells[row, 3].Value = fileInfo.Length;
        ws.Cells[row, 4].Value = fileInfo.Attributes.ToString();
        ws.Row(row).OutlineLevel = outlineLevel;
        row++;

        allFiles.Add(fileInfo);
    }
    catch (IOException)
    {
        Console.WriteLine($"Nie można odczytać pliku: {path}");
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data;
using System.IO;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;
using System.Net.Http.Headers;
using System.Windows.Media.Imaging;
//using Microsoft.Office.Core;

namespace ExcelClass
{
    public class Class1
    {
        

        private string _filePath;
        private string _nameFile;

        public Workbook _book { get; set; }

        protected Excel.Application _xL { get; set; }

        protected Worksheet _sheet { get; set; }

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
            }
        }
        public string NameFile
        {
            get
            {
                return _nameFile;
            }
            set
            {
                _nameFile = value;
            }
        }

        public Class1()
        {
             _xL = new Excel.Application();
            _book = _xL.Workbooks.Add();
            _sheet = _book.ActiveSheet;


        }

        public bool OpenFile(string filePath, bool create)
        {
            try
            {
                bool f = File.Exists(@filePath);
                if (File.Exists(@filePath) != create)
                {

                    FilePath = filePath;
                    //NameFile = nameFile;

                    if (File.Exists(FilePath))
                    {
                        _book = _xL.Workbooks.Open(FilePath, 0, false, 5, "", "", false, XlPlatform.xlWindows, "", true, false, 0, true, false, false);
                    }
                    else
                        _book = _xL.Workbooks.Add(Type.Missing);

                    return true;
                }
                return false;


            }
            catch (Exception e)
            {
                throw new Exception("Problema apertura file Excel" + e.Message);
            }
        }


        public void AddSheet(string name, bool first)
        {
            //active the sheet if it is the first, otherwise create a new sheet
            if (first)
            {
                _sheet = _book.ActiveSheet as Worksheet;
            }
            else
            {
                object missing = Type.Missing;
                _sheet = _book.Sheets.Add(missing, missing, 1, missing) as Worksheet;
            }
            _sheet.Name = name;
        }

        //makes the current sheet the active sheet.
        public void SetSheet(string name)
        {
            object missing = Type.Missing;
            _sheet = _book.Sheets.get_Item(name) as Microsoft.Office.Interop.Excel.Worksheet;
            _sheet.Activate();
        }

        public Worksheet GetSheet(string name)
        {
            _sheet = _book.Sheets.get_Item(name) as Microsoft.Office.Interop.Excel.Worksheet;
            return _sheet as Excel.Worksheet;
        }

        public void SetTitleIntoSheet(Worksheet _sheet, string title, int cont)
        {
            int row = (cont * 5) + 5;
            _sheet.Cells[row,15] = title;
        }

        public void SetImageIntoSheet(Worksheet _sheeet, string image, int cont)
        {
            string path_image = "C:\\Users\\lenovo\\source\\repos\\HMIver2\\HMI\\bin\\Debug\\net6.0-windows\\" + image;
            _sheet.Shapes.AddPicture(path_image, Microsoft.Office.Core.MsoTriState.msoFalse, Microsoft.Office.Core.MsoTriState.msoCTrue, 10, (350 * cont) + 30 , 600, 350);
        }
        

        public void CloseFile()
        {
            object missing = Type.Missing;
            if (File.Exists(FilePath))
            {
                _xL.DisplayAlerts = false;
                _book.SaveAs(FilePath, _book.FileFormat,
                   missing, missing, missing, missing,
                   Excel.XlSaveAsAccessMode.xlNoChange,
                   missing, missing, missing, missing, missing);
            }
            else
            {
                _xL.DisplayAlerts = false;

                _book.SaveAs(FilePath, Excel.XlFileFormat.xlExcel7,
                  missing, missing, missing, missing,
                  Excel.XlSaveAsAccessMode.xlNoChange,
                  missing, missing, missing, missing, missing);

            }
            _book.Close(missing, missing, missing);
            Close();

        }

        public void Close()
        {

            if (_xL != null)
            {
                _sheet = null;
                _xL.UserControl = false;
                _xL.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_xL);
                Dispose();

            }
        }

        public void Dispose()
        {
            _book = null;
            _xL = null;
        }
    }

}


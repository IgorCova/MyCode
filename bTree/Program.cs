using System;
using System.IO;
using System.Linq;

namespace bTree
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] numbers = { 5, 3, 7, 6, 8, 10, 9, 4, 2, 13, 0 };
            int[] inputNumbers = {};
            Console.WriteLine("Введите в консоль последовательность чисел через пробел, либо введите название файла (например 'file: nodes.txt') и нажмите Enter:");
            string line = Console.ReadLine();

            if (line.IndexOf("file: ") == 0)
            {
                string path = line.Substring(6);
                FileInfo fileInf = new FileInfo(path);

                // проверка на существование файла
                if (!fileInf.Exists)
                {
                    Console.WriteLine("Файл с названием: '{0}' не существует", path);
                    return;
                }

                string textFromFile = "";
                // чтение из файла
                using (FileStream fstream = File.OpenRead(path))
                {
                    // преобразуем строку в байты
                    byte[] array = new byte[fstream.Length];
                    // считываем данные
                    fstream.Read(array, 0, array.Length);
                    // декодируем байты в строку
                    textFromFile = System.Text.Encoding.Default.GetString(array);
                    Console.WriteLine($"Текст из файла: \n{textFromFile}");

                    inputNumbers = GetNodesFromString(line);
                }
            }
            else
            {
                inputNumbers = GetNodesFromString(line);
            }

            if (inputNumbers.Length > 0) 
            {
                Console.WriteLine("Последовательность чисел не введена используется последовательность по умолчанию");
                numbers = inputNumbers;
            }
            // Инициализация биинарного дерева поиска
            BSTree binaryTree = new BSTree();

            // Добавления узлов в дерево
            foreach (int node in numbers)
            {
                binaryTree.Add(node);
            }

            Console.WriteLine("Бинарное дерево поиска:");
            binaryTree.Root.Print();

            int arithmeticalMean = binaryTree.GetArithmeticalMean();
            Console.WriteLine("\nСреднее арифметическое внутренних узлов сформированного дерева: {0}\n", arithmeticalMean);

            binaryTree.Dispose();
            Console.ReadLine();
        }

        private static int[] GetNodesFromString(string line)
        {
            try
            {
                // Форматируем строку записанную в консоль или в файл в массив чисел
                int[] inNumbers = line.Split(' ').Select(Int32.Parse).ToArray();

                return inNumbers;
            }
            catch
            {
                Console.WriteLine("Введена строка должна быть последовательностью чисел через пробел");
            };

            return new int[] { };
        }
    }
}

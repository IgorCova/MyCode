using System;

namespace graph
{
    class Program
    {
        static void Main(string[] args)
        {
            // Инициализация пустого графа
            Graph g = new Graph();

            // Добавление вершин
            g.AddVertex("A");
            g.AddVertex("B");
            g.AddVertex("C");
            g.AddVertex("D");
            g.AddVertex("E");
            g.AddVertex("F");
            g.AddVertex("G");
            g.AddVertex("H");

            // Добавление ребер
            g.AddEdge("A", "B", 22);
            g.AddEdge("A", "C", 33);
            g.AddEdge("A", "D", 61);
            g.AddEdge("B", "C", 47);
            g.AddEdge("B", "E", 93);
            g.AddEdge("C", "D", 11);
            g.AddEdge("C", "E", 79);
            g.AddEdge("C", "F", 63);
            g.AddEdge("D", "F", 41);
            g.AddEdge("E", "F", 17);
            g.AddEdge("E", "G", 58);
            g.AddEdge("F", "G", 84);
            g.AddEdge("H", "G", 10);

            // Инициализация класса с алгоритом поиска Дейкстра с графом
            Dijkstra dijkstra = new Dijkstra(g);
            string shortestPath = dijkstra.FindShortestPath("A", "H");
            Console.WriteLine("Найден кротчайший путь: {0}", string.Join(" -> ", shortestPath.ToCharArray()));
            Console.ReadLine();
        }
    }
}

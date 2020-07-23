using System.Collections.Generic;

// Алгоритм Дейкстры
public class Dijkstra
{
    Graph graph;

    List<GraphVertexInfo> infos;

    // Конструктор
    // graph - Граф
    public Dijkstra(Graph graph)
    {
        this.graph = graph;
    }

    // Инициализация информации
    void InitInfo()
    {
        infos = new List<GraphVertexInfo>();
        foreach (var v in graph.Vertices)
        {
            infos.Add(new GraphVertexInfo(v));
        }
    }

    // Получение информации о вершине графа
    // v - Вершина
    // Возвращает Информацию о вершине
    GraphVertexInfo GetVertexInfo(GraphVertex v)
    {
        foreach (var i in infos)
        {
            if (i.Vertex.Equals(v))
            {
                return i;
            }
        }

        return null;
    }

    // Поиск непосещенной вершины с минимальным значением суммы
    // Возвращает Информацию о вершине
    public GraphVertexInfo FindUnvisitedVertexWithMinSum()
    {
        var minValue = int.MaxValue;
        GraphVertexInfo minVertexInfo = null;
        foreach (var i in infos)
        {
            if (i.IsUnvisited && i.EdgesWeightSum < minValue)
            {
                minVertexInfo = i;
                minValue = i.EdgesWeightSum;
            }
        }

        return minVertexInfo;
    }

    // Поиск кратчайшего пути по названиям вершин
    // startName - Название стартовой вершины
    // finishName - Название финишной вершины
    // Возвращает Кратчайший путь
    public string FindShortestPath(string startName, string finishName)
    {
        return FindShortestPath(graph.FindVertex(startName), graph.FindVertex(finishName));
    }

    // Поиск кратчайшего пути по вершинам
    // startVertex - Стартовая вершина
    // finishVertex - Финишная вершина
    // Возвращает Кратчайший путь
    public string FindShortestPath(GraphVertex startVertex, GraphVertex finishVertex)
    {
        InitInfo();
        var first = GetVertexInfo(startVertex);
        first.EdgesWeightSum = 0;
        while (true)
        {
            var current = FindUnvisitedVertexWithMinSum();
            if (current == null)
            {
                break;
            }

            SetSumToNextVertex(current);
        }

        return GetPath(startVertex, finishVertex);
    }

    // Вычисление суммы весов ребер для следующей вершины
    // info - Информация о текущей вершине
    void SetSumToNextVertex(GraphVertexInfo info)
    {
        info.IsUnvisited = false;
        foreach (var e in info.Vertex.Edges)
        {
            var nextInfo = GetVertexInfo(e.ConnectedVertex);
            var sum = info.EdgesWeightSum + e.EdgeWeight;
            if (sum < nextInfo.EdgesWeightSum)
            {
                nextInfo.EdgesWeightSum = sum;
                nextInfo.PreviousVertex = info.Vertex;
            }
        }
    }

    // Формирование пути
    // startVertex - Начальная вершина
    // endVertex - Конечная вершина
    // Возвращает Путь
    string GetPath(GraphVertex startVertex, GraphVertex endVertex)
    {
        var path = endVertex.ToString();
        while (startVertex != endVertex)
        {
            endVertex = GetVertexInfo(endVertex).PreviousVertex;
            path = endVertex.ToString() + path;
        }

        return path;
    }
}
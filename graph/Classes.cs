using System.Collections.Generic;

// Информация о вершине
public class GraphVertexInfo
{
    // Вершина
    public GraphVertex Vertex { get; set; }

    // Не посещенная вершина
    public bool IsUnvisited { get; set; }

    // Сумма весов ребер
    public int EdgesWeightSum { get; set; }

    // Предыдущая вершина
    public GraphVertex PreviousVertex { get; set; }

    // Конструктор
    public GraphVertexInfo(GraphVertex vertex) // vertex - Вершина
    {
        Vertex = vertex;
        IsUnvisited = true;
        EdgesWeightSum = int.MaxValue;
        PreviousVertex = null;
    }
}

// Ребро графа
public class GraphEdge
{
    // Связанная вершина
    public GraphVertex ConnectedVertex { get; }

    
    // Вес ребра
    public int EdgeWeight { get; }

    
    // Конструктор
    // connectedVertex - Связанная вершина
    // weight - Вес ребра
    public GraphEdge(GraphVertex connectedVertex, int weight)
    {
        ConnectedVertex = connectedVertex;
        EdgeWeight = weight;
    }
}

// Вершина графа
public class GraphVertex
{   
    // Название вершины
    public string Name { get; }

    // Список ребер
    public List<GraphEdge> Edges { get; }

    
    // Конструктор
    // vertexName - Название вершины
    public GraphVertex(string vertexName)
    {
        Name = vertexName;
        Edges = new List<GraphEdge>();
    }
    
    // Добавить ребро
    // newEdge - Ребро
    public void AddEdge(GraphEdge newEdge)
    {
        Edges.Add(newEdge);
    }

    
    // Добавить ребро
    // vertex - Вершина
    // edgeWeight - Вес
    public void AddEdge(GraphVertex vertex, int edgeWeight)
    {
        AddEdge(new GraphEdge(vertex, edgeWeight));
    }

    // Преобразование в строку возвращает Имя вершины
    public override string ToString() => Name;
}

// Граф
public class Graph
{
    // Список вершин графа
    public List<GraphVertex> Vertices { get; }

    // Конструктор
    public Graph()
    {
        Vertices = new List<GraphVertex>();
    }
    
    // Добавление вершины
    // vertexName - Имя вершины
    public void AddVertex(string vertexName)
    {
        Vertices.Add(new GraphVertex(vertexName));
    }

    // Поиск вершины
    // vertexName - Название вершины
    // Возвращает Найденную вершину
    public GraphVertex FindVertex(string vertexName)
    {
        foreach (var v in Vertices)
        {
            if (v.Name.Equals(vertexName))
            {
                return v;
            }
        }

        return null;
    }
    
    // Добавление ребра
    // firstName - Имя первой вершины
    // secondName - Имя второй вершины
    // weight - Вес ребра соединяющего вершины
    public void AddEdge(string firstName, string secondName, int weight)
    {
        var v1 = FindVertex(firstName);
        var v2 = FindVertex(secondName);
        if (v2 != null && v1 != null)
        {
            v1.AddEdge(v2, weight);
            v2.AddEdge(v1, weight);
        }
    }
}
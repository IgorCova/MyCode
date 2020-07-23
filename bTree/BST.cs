// Узел бинарного дерева
using System;

public class BNode : IDisposable
{
    public BNode LeftNode { get; set; }
    public BNode RightNode { get; set; }
    public int Value { get; set; }
    private bool disposed = false;
    // Конструктор сласса BNode
    public BNode(int value)
    {
        Value = value;
    }

    // Метод для деструктора
    public void Dispose()
    {
        Dispose(true);
        Console.WriteLine("Удаление узла: {0}", Value);
        // Этот объект будет очищен методом Dispose. 
        // Следовательно, вам следует вызвать GC.SupressFinalize, 
        // чтобы убрать этот объект из очереди завершения и не допустить 
        // повторного выполнения кода завершения для этого объекта.
        GC.SuppressFinalize(this);
    }

    public void DisposeWithLeafs()
    {        
        if (this.LeftNode != null) {
            this.LeftNode.DisposeWithLeafs();
        }

        if (this.RightNode != null) {
            this.RightNode.DisposeWithLeafs();
        }

        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Проверка, не был ли вызван Dispose.
        if (!this.disposed)
        {
            disposed = true; // Отмечаем что был вызван метод Dispose
            Dispose();
        }
    }

    // Деструктор сласса BNode
    ~BNode()
    {
        Dispose(false);
    }
}

// Бинарное дерево поиска
public class BSTree : IDisposable
{
    // Корневой узел
    public BNode Root { get; set; }
    private bool disposed = false;

    // Добавление узла в дерево
    public bool Add(int value)
    {
        BNode before = null;
        BNode after = this.Root;

        while (after != null)
        {
            before = after;
            if (value < after.Value) // Новый узел добавить слева 
            {
                after = after.LeftNode;
            }
            else if (value > after.Value) // Новый узел добавить справа 
            {
                after = after.RightNode;
            }
            else // Такое значение в дереве уже есть
            {
                return false;
            }
        }

        BNode newNode = new BNode(value);

        if (this.Root == null) // Дерево пустое
        {
            this.Root = newNode;
        }
        else
        {
            if (value < before.Value)
            {
                before.LeftNode = newNode;
            }
            else
            {
                before.RightNode = newNode;
            }
        }

        return true;
    }

    // Метод для деструктора
    public void Dispose()
    {
        Console.WriteLine("Очистка памяти дерева, удалаяем все узлы рекурсивно начиная с листовых узлов:");
        Root.DisposeWithLeafs();
        Dispose(true);
        // Этот объект будет очищен методом Dispose. 
        // Следовательно, вам следует вызвать GC.SupressFinalize, 
        // чтобы убрать этот объект из очереди завершения и не допустить 
        // повторного выполнения кода завершения для этого объекта.
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Проверка, не был ли вызван Dispose.
        if (!this.disposed)
        {
            disposed = true; // Отмечаем что был вызван метод Dispose
        }
    }
}
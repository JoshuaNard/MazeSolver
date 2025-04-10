using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace MazeSolverApp
{
    // Custom panel with double-buffering enabled.
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel() { this.DoubleBuffered = true; }
    }

    public partial class MazeSolver : Form
    {
        private Maze maze = null!;
        private DoubleBufferedPanel panelMaze = null!;
        private ComboBox comboAlgorithm = null!;
        private ComboBox comboMazeSize = null!;
        private TrackBar trackBarDelay = null!;
        private Button btnNewMaze = null!;
        private Button btnRestartMaze = null!;
        private Button btnPausePlay = null!;
        private Label lblStepCounter = null!;
        // Removed lblProgress field.
        private WinFormsTimer timer = null!;
        private int stepCount = 0;
        private bool isPaused = false;
        private IEnumerator<bool> solverEnumerator = null!;
        private string selectedAlgorithm = "DFS";
        private int mazeRows = 20;
        private int mazeCols = 20;

        public MazeSolver() : base()
        {
            InitializeComponent();
            CreateControls();
            GenerateNewMaze();
        }

        private void InitializeComponent()
        {
            this.Text = "2D Maze Solver";
            this.Width = 800;
            this.Height = 800;
            // Optionally enable form double-buffering:
            this.DoubleBuffered = true;
        }

        private void CreateControls()
        {
            // Create a TableLayoutPanel that fills the form.
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 1;
            table.RowCount = 2;
            // First row is for the control panel with fixed height.
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            // Second row takes the remaining space.
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Create the top control panel.
            Panel controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Fill;

            // Algorithm selection drop-down.
            comboAlgorithm = new ComboBox();
            comboAlgorithm.Items.AddRange(new string[] { "DFS", "BFS", "A*" });
            comboAlgorithm.SelectedIndex = 0;
            comboAlgorithm.Location = new Point(10, 10);
            comboAlgorithm.Width = 100;
            comboAlgorithm.SelectedIndexChanged += (s, e) =>
            {
                selectedAlgorithm = comboAlgorithm.SelectedItem.ToString();
            };
            controlPanel.Controls.Add(comboAlgorithm);

            // Maze size drop-down.
            comboMazeSize = new ComboBox();
            comboMazeSize.Items.AddRange(new string[] { "Small (20x20)", "Medium (50x50)", "Large (100x100)" });
            comboMazeSize.SelectedIndex = 0;
            comboMazeSize.Location = new Point(120, 10);
            comboMazeSize.Width = 140;
            comboMazeSize.SelectedIndexChanged += (s, e) =>
            {
                switch (comboMazeSize.SelectedIndex)
                {
                    case 0:
                        mazeRows = 20;
                        mazeCols = 20;
                        break;
                    case 1:
                        mazeRows = 50;
                        mazeCols = 50;
                        break;
                    case 2:
                        mazeRows = 100;
                        mazeCols = 100;
                        break;
                }
                GenerateNewMaze();
            };
            controlPanel.Controls.Add(comboMazeSize);

            // TrackBar for setting delay.
            trackBarDelay = new TrackBar();
            trackBarDelay.Minimum = 1;  // Minimum of 1 ms to avoid zero delay.
            trackBarDelay.Maximum = 1000;
            trackBarDelay.TickFrequency = 100;
            trackBarDelay.Value = 250; // Set a moderate default delay
            trackBarDelay.Location = new Point(270, 10);
            trackBarDelay.Width = 200;
            controlPanel.Controls.Add(trackBarDelay);

            // Label for the delay TrackBar.
            Label lblDelay = new Label();
            lblDelay.Text = "Delay (ms):";
            lblDelay.Location = new Point(270, 40); // Adjust Y value as needed.
            lblDelay.AutoSize = true;
            controlPanel.Controls.Add(lblDelay);

            // New Maze button.
            btnNewMaze = new Button();
            btnNewMaze.Text = "New Maze";
            btnNewMaze.Location = new Point(480, 10);
            btnNewMaze.Click += (s, e) => { GenerateNewMaze(); };
            controlPanel.Controls.Add(btnNewMaze);

            // Restart Maze button.
            btnRestartMaze = new Button();
            btnRestartMaze.Text = "Restart Maze";
            btnRestartMaze.Location = new Point(580, 10);
            btnRestartMaze.Click += (s, e) => { RestartSolver(); };
            controlPanel.Controls.Add(btnRestartMaze);

            // Pause/Play button.
            btnPausePlay = new Button();
            btnPausePlay.Text = "Pause";
            btnPausePlay.Location = new Point(680, 10);
            btnPausePlay.Click += (s, e) => { TogglePause(); };
            controlPanel.Controls.Add(btnPausePlay);

            // Step counter label.
            lblStepCounter = new Label();
            lblStepCounter.Text = "Steps: 0";
            lblStepCounter.Location = new Point(10, 40);
            lblStepCounter.AutoSize = true;
            controlPanel.Controls.Add(lblStepCounter);

            // Remove lblProgress; do not add it.

            // Add controlPanel to row 0 of the table.
            table.Controls.Add(controlPanel, 0, 0);

            // Create the maze drawing panel using the double-buffered panel.
            panelMaze = new DoubleBufferedPanel();
            panelMaze.Dock = DockStyle.Fill;
            panelMaze.BackColor = Color.White;
            panelMaze.Paint += PanelMaze_Paint;
            // Add panelMaze to row 1 of the table.
            table.Controls.Add(panelMaze, 0, 1);

            // Add the table to the form.
            this.Controls.Add(table);

            // Timer initialization.
            timer = new WinFormsTimer();
            timer.Interval = trackBarDelay.Value;
            timer.Tick += Timer_Tick;
            trackBarDelay.Scroll += (s, e) =>
            {
                timer.Interval = trackBarDelay.Value;
            };
        }

        private void GenerateNewMaze()
        {
            timer.Stop();
            stepCount = 0;
            lblStepCounter.Text = "Steps: 0";
            // Removed lblProgress update.
            maze = new Maze(mazeRows, mazeCols);
            solverEnumerator = GetSolverEnumerator();
            panelMaze.Invalidate();
            timer.Start();
        }

        private void RestartSolver()
        {
            timer.Stop();
            stepCount = 0;
            lblStepCounter.Text = "Steps: 0";
            // Removed lblProgress update.
            // Reset cell statuses in the current maze.
            for (int i = 0; i < maze.Rows; i++)
                for (int j = 0; j < maze.Cols; j++)
                    maze.Cells[i, j].Status = CellStatus.Unvisited;
            solverEnumerator = GetSolverEnumerator();
            panelMaze.Invalidate();
            timer.Start();
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            if (isPaused)
            {
                timer.Stop();
                btnPausePlay.Text = "Play";
            }
            else
            {
                timer.Start();
                btnPausePlay.Text = "Pause";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (isPaused) return;
            if (solverEnumerator != null)
            {
                try
                {
                    if (!solverEnumerator.MoveNext())
                    {
                        timer.Stop();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Solver error: " + ex.Message);
                    timer.Stop();
                }
                stepCount++;
                lblStepCounter.Text = "Steps: " + stepCount;
                // Removed call to UpdateProgressDisplay().
                panelMaze.Invalidate();
            }
        }

        // Removed UpdateProgressDisplay method.

        // Choose the solver enumerator based on the selected algorithm.
        private IEnumerator<bool> GetSolverEnumerator()
        {
            switch (selectedAlgorithm)
            {
                case "BFS":
                    return SolveBFS();
                case "A*":
                    return SolveAStar();
                case "DFS":
                default:
                    return SolveDFS();
            }
        }

        // --------------------- Solver Enumerators --------------------------

        // DFS solver that yields each step.
        private IEnumerator<bool> SolveDFS()
        {
            MazeCell start = maze.Cells[0, 0];
            MazeCell goal = maze.Cells[maze.Rows - 1, maze.Cols - 1];

            // Reset the solver visitation flag for all cells.
            foreach (MazeCell cell in maze.Cells)
            {
                cell.SolverVisited = false;
            }

            // Stack element: (cell, nextNeighborIndex)
            Stack<(MazeCell cell, int nextIndex)> stack = new Stack<(MazeCell, int)>();

            // Start the DFS with the start cell.
            start.SolverVisited = true;
            stack.Push((start, 0));
            yield return true;

            while (stack.Count > 0)
            {
                // Update the visualization for the current branch:
                // The top (active) cell is marked as Current (purple), others as Explored (light blue).
                var branch = stack.ToArray(); // Array with top at index 0.
                for (int i = 0; i < branch.Length; i++)
                {
                    if (i == 0)
                        branch[i].cell.Status = CellStatus.Current;  // Active cell purple.
                    else
                        branch[i].cell.Status = CellStatus.Explored;   // Tentative branch light blue.
                }
                yield return true;

                var top = stack.Peek();
                MazeCell current = top.cell;
                List<MazeCell> neighbors = GetReachableNeighbors(current);

                // If goal is reached, mark entire branch as Final (blue) and exit.
                if (current == goal)
                {
                    foreach (var entry in stack)
                    {
                        entry.cell.Status = CellStatus.Final;
                        yield return true;
                    }
                    yield break;
                }

                // If there are still neighbors to try from current cell.
                if (top.nextIndex < neighbors.Count)
                {
                    MazeCell neighbor = neighbors[top.nextIndex];
                    stack.Pop();
                    stack.Push((current, top.nextIndex + 1));
                    if (!neighbor.SolverVisited)
                    {
                        neighbor.SolverVisited = true;
                        stack.Push((neighbor, 0));
                        yield return true;
                    }
                }
                else
                {
                    // No more neighbors: current cell is dead end.
                    if (current != start && current != goal)
                    {
                        current.Status = CellStatus.Unvisited;
                        yield return true;
                    }
                    stack.Pop();
                    yield return true;
                }
            }
            yield break;
        }

        // BFS solver that yields each step.
        private IEnumerator<bool> SolveBFS()
        {
            Queue<MazeCell> queue = new Queue<MazeCell>();
            MazeCell start = maze.Cells[0, 0];
            MazeCell goal = maze.Cells[maze.Rows - 1, maze.Cols - 1];
            Dictionary<MazeCell, MazeCell> parent = new Dictionary<MazeCell, MazeCell>();
            queue.Enqueue(start);
            start.Status = CellStatus.Current;
            yield return true;
            while (queue.Count > 0)
            {
                MazeCell current = queue.Dequeue();
                current.Status = CellStatus.Current;
                yield return true;
                if (current == goal)
                {
                    MazeCell temp = goal;
                    while (temp != null)
                    {
                        temp.Status = CellStatus.Final;
                        parent.TryGetValue(temp, out temp);
                        yield return true;
                    }
                    yield break;
                }
                foreach (MazeCell neighbor in GetReachableNeighbors(current))
                {
                    if (neighbor.Status == CellStatus.Unvisited)
                    {
                        neighbor.Status = CellStatus.Current;
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                        yield return true;
                    }
                }
                if (current.Status != CellStatus.Final)
                    current.Status = CellStatus.Explored;
                yield return true;
            }
            yield break;
        }

        // A* solver that yields each step.
        private IEnumerator<bool> SolveAStar()
        {
            MazeCell start = maze.Cells[0, 0];
            MazeCell goal = maze.Cells[maze.Rows - 1, maze.Cols - 1];

            SortedSet<(int, int, MazeCell)> openSet =
                new SortedSet<(int, int, MazeCell)>(Comparer<(int, int, MazeCell)>.Create((a, b) =>
                {
                    int comp = a.Item1.CompareTo(b.Item1);
                    if (comp == 0)
                    {
                        comp = a.Item2.CompareTo(b.Item2);
                    }
                    return comp;
                }));

            Dictionary<MazeCell, MazeCell> parent = new Dictionary<MazeCell, MazeCell>();
            Dictionary<MazeCell, int> gScore = new Dictionary<MazeCell, int>();
            int counter = 0;
            foreach (MazeCell cell in maze.Cells)
            {
                gScore[cell] = int.MaxValue;
            }
            gScore[start] = 0;
            int fScoreStart = Heuristic(start, goal);
            openSet.Add((fScoreStart, counter, start));
            start.Status = CellStatus.Current;
            yield return true;

            while (openSet.Count > 0)
            {
                var currentTuple = openSet.Min;
                openSet.Remove(currentTuple);
                MazeCell current = currentTuple.Item3;
                current.Status = CellStatus.Current;
                yield return true;

                if (current == goal)
                {
                    MazeCell temp = goal;
                    while (temp != null)
                    {
                        temp.Status = CellStatus.Final;
                        parent.TryGetValue(temp, out temp);
                        yield return true;
                    }
                    yield break;
                }

                foreach (MazeCell neighbor in GetReachableNeighbors(current))
                {
                    int tentative_gScore = gScore[current] + 1;
                    if (tentative_gScore < gScore[neighbor])
                    {
                        parent[neighbor] = current;
                        gScore[neighbor] = tentative_gScore;
                        int fScore = tentative_gScore + Heuristic(neighbor, goal);
                        counter++;
                        openSet.Add((fScore, counter, neighbor));
                        neighbor.Status = CellStatus.Current;
                        yield return true;
                    }
                }
                if (current.Status != CellStatus.Final)
                    current.Status = CellStatus.Explored;
                yield return true;
            }
            yield break;
        }

        // Manhattan distance heuristic.
        private int Heuristic(MazeCell cell, MazeCell goal)
        {
            return Math.Abs(cell.Row - goal.Row) + Math.Abs(cell.Col - goal.Col);
        }

        // Get reachable neighbors by checking if the wall in that direction is absent.
        private List<MazeCell> GetReachableNeighbors(MazeCell cell)
        {
            List<MazeCell> neighbors = new List<MazeCell>();
            int r = cell.Row, c = cell.Col;
            if (!cell.TopWall && r > 0)
                neighbors.Add(maze.Cells[r - 1, c]);
            if (!cell.BottomWall && r < maze.Rows - 1)
                neighbors.Add(maze.Cells[r + 1, c]);
            if (!cell.LeftWall && c > 0)
                neighbors.Add(maze.Cells[r, c - 1]);
            if (!cell.RightWall && c < maze.Cols - 1)
                neighbors.Add(maze.Cells[r, c + 1]);
            return neighbors;
        }

        // ---------------------- Painting the Maze --------------------------
        private void PanelMaze_Paint(object? sender, PaintEventArgs e)
        {
            if (maze == null)
                return;

            Graphics g = e.Graphics;
            int cellWidth = panelMaze.Width / maze.Cols;
            int cellHeight = panelMaze.Height / maze.Rows;
            int mazeWidth = maze.Cols * cellWidth;
            int mazeHeight = maze.Rows * cellHeight;
            int offsetX = (panelMaze.Width - mazeWidth) / 2;
            int offsetY = (panelMaze.Height - mazeHeight) / 2;

            for (int i = 0; i < maze.Rows; i++)
            {
                for (int j = 0; j < maze.Cols; j++)
                {
                    MazeCell cell = maze.Cells[i, j];
                    int x = offsetX + j * cellWidth;
                    int y = offsetY + i * cellHeight;
                    Color fillColor = Color.White;
                    switch (cell.Status)
                    {
                        case CellStatus.Unvisited:
                            fillColor = Color.White;
                            break;
                        case CellStatus.Explored:
                            fillColor = Color.LightBlue;
                            break;
                        case CellStatus.Current:
                            fillColor = Color.MediumPurple;
                            break;
                        case CellStatus.Final:
                            fillColor = Color.Blue;
                            break;
                    }
                    using (SolidBrush brush = new SolidBrush(fillColor))
                    {
                        g.FillRectangle(brush, x, y, cellWidth, cellHeight);
                    }
                    Pen wallPen = Pens.Black;
                    if (cell.TopWall)
                        g.DrawLine(wallPen, x, y, x + cellWidth, y);
                    if (cell.LeftWall)
                        g.DrawLine(wallPen, x, y, x, y + cellHeight);
                    if (cell.RightWall)
                        g.DrawLine(wallPen, x + cellWidth, y, x + cellWidth, y + cellHeight);
                    if (cell.BottomWall)
                        g.DrawLine(wallPen, x, y + cellHeight, x + cellWidth, y + cellHeight);
                }
            }
            
            // Draw start and end markers.
            int startX = offsetX;
            int startY = offsetY;
            Rectangle startRect = new Rectangle(startX, startY, cellWidth, cellHeight);
            g.FillRectangle(Brushes.Red, startRect);
            g.DrawRectangle(Pens.Black, startRect);

            int endX = offsetX + (maze.Cols - 1) * cellWidth;
            int endY = offsetY + (maze.Rows - 1) * cellHeight;
            Rectangle endRect = new Rectangle(endX, endY, cellWidth, cellHeight);
            g.FillRectangle(Brushes.Green, endRect);
            g.DrawRectangle(Pens.Black, endRect);

            // Removed debug step counter overlay.
        }
    }

    // Defines the visual status of a cell.
    public enum CellStatus
    {
        Unvisited,  // white
        Explored,   // light blue for the in-progress search branch
        Current,    // purple for the active search cell
        Final       // blue for the final solution path
    }

    // Represents one cell in the maze.
    public class MazeCell
    {
        public int Row, Col;
        public bool TopWall = true, RightWall = true, BottomWall = true, LeftWall = true;
        public bool Visited = false;
        public CellStatus Status = CellStatus.Unvisited;
        public bool SolverVisited = false;

        public MazeCell(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    // Maze class that holds the grid and generates the maze using recursive backtracking.
    public class Maze
    {
        public int Rows, Cols;
        public MazeCell[,] Cells;
        private Random rand = new Random();

        public Maze(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Cells = new MazeCell[Rows, Cols];
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Cells[i, j] = new MazeCell(i, j);
                }
            }
            GenerateMaze();
        }

        public void GenerateMaze()
        {
            Stack<MazeCell> stack = new Stack<MazeCell>();
            MazeCell current = Cells[0, 0];
            current.Visited = true;

            do
            {
                List<MazeCell> neighbors = GetUnvisitedNeighbors(current);
                if (neighbors.Count > 0)
                {
                    MazeCell chosen = neighbors[rand.Next(neighbors.Count)];
                    RemoveWall(current, chosen);
                    stack.Push(current);
                    current = chosen;
                    current.Visited = true;
                }
                else if (stack.Count > 0)
                {
                    current = stack.Pop();
                }
            } while (stack.Count > 0);

            // Reset generation flags and statuses for solver use.
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    Cells[i, j].Visited = false;
                    Cells[i, j].Status = CellStatus.Unvisited;
                }
        }

        private List<MazeCell> GetUnvisitedNeighbors(MazeCell cell)
        {
            List<MazeCell> neighbors = new List<MazeCell>();
            int row = cell.Row;
            int col = cell.Col;
            if (row > 0 && !Cells[row - 1, col].Visited)
                neighbors.Add(Cells[row - 1, col]);
            if (row < Rows - 1 && !Cells[row + 1, col].Visited)
                neighbors.Add(Cells[row + 1, col]);
            if (col > 0 && !Cells[row, col - 1].Visited)
                neighbors.Add(Cells[row, col - 1]);
            if (col < Cols - 1 && !Cells[row, col + 1].Visited)
                neighbors.Add(Cells[row, col + 1]);
            return neighbors;
        }

        private void RemoveWall(MazeCell current, MazeCell next)
        {
            int dRow = next.Row - current.Row;
            int dCol = next.Col - current.Col;
            if (dRow == 1)
            {
                current.BottomWall = false;
                next.TopWall = false;
            }
            else if (dRow == -1)
            {
                current.TopWall = false;
                next.BottomWall = false;
            }
            else if (dCol == 1)
            {
                current.RightWall = false;
                next.LeftWall = false;
            }
            else if (dCol == -1)
            {
                current.LeftWall = false;
                next.RightWall = false;
            }
        }
    }
}

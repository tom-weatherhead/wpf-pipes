using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;             // In the assembly PresentationCore.  Also requires the assembly WindowsBase.
using System.Windows.Media.Imaging;     // In the assembly PresentationCore.  Also requires the assembly WindowsBase.
using System.Text;

#if JAVASCRIPT_CODE
// Pipes - Script.js - Javascript - February 5, 2011

// **** Global Variable Declarations ****

var nMinGridWidth = 2;
var nMinGridHeight = 2;
var nDefaultGridWidth = 8;
var nDefaultGridHeight = 8;
var nMaxGridWidth = 20;
var nMaxGridHeight = 20;

// **** Function Declarations ****

function setGridDimensions() {
    var vars = getURLParameters();

    nGridWidth = getURLParameterAsLimitedInt(vars, "GridWidth", nMinGridWidth, nMaxGridWidth, nDefaultGridWidth);
    nGridHeight = getURLParameterAsLimitedInt(vars, "GridHeight", nMinGridHeight, nMaxGridHeight, nDefaultGridHeight);

    nGridArea = nGridWidth * nGridHeight;
    aGridImageNumbers = new Array(nGridArea);
    abImageIsGreen = new Array(nGridArea);
}

function constructGrid() {
    var i = 0;

    prepareForNewGame();
    setGridDimensions();
    setGridImageNumbers();
    findGreenTree();

    for (var row = 0; row < nGridHeight; row++) {
        $("<tr></tr>").appendTo("#pipeGrid");

        for (var col = 0; col < nGridWidth; col++) {
            var strImgSrc = constructImageSourceString(i);

            //$("<td><img name='btnGridImage' id='btnGridImage" + i + "' src='" + strImgSrc + "' alt='" + i + "' onclick='imageClicked(" + i + ")' /></td>").appendTo("#pipeGrid tr:last");
            $("<td><img name='btnGridImage' src='" + strImgSrc + "' onclick='imageClicked(" + i + ")' /></td>").appendTo("#pipeGrid tr:last");
            i++;
        }
    }
}

function resizeGrid() {
    var tbWidth = document.getElementById("txtWidth");
    var tbHeight = document.getElementById("txtHeight");
    var strWidth = tbWidth.value;
    var strHeight = tbHeight.value;
    var nWidth = nDefaultGridWidth;
    var nHeight = nDefaultGridHeight;

    if (strWidth != null && strWidth != "") {
        nWidth = parseInt(strWidth, 10);

        if (isNaN(nWidth)) {
            alert("Error: Width is not a number");
            return;
        }

        if (nWidth < nMinGridWidth) {
            alert("Warning: Minimum width is " + nMinGridWidth);
            nWidth = nMinGridWidth;
        } else if (nWidth > nMaxGridWidth) {
            alert("Warning: Maximum width is " + nMaxGridWidth);
            nWidth = nMaxGridWidth;
        }
    }

    if (strHeight != null && strHeight != "") {
        nHeight = parseInt(strHeight, 10);

        if (isNaN(nHeight)) {
            alert("Error: Height is not a number");
            return;
        }

        if (nHeight < nMinGridHeight) {
            alert("Warning: Minimum height is " + nMinGridHeight);
            nHeight = nMinGridHeight;
        } else if (nHeight > nMaxGridHeight) {
            alert("Warning: Maximum height is " + nMaxGridHeight);
            nHeight = nMaxGridHeight;
        }
    }

    window.location = "index.html?GridWidth=" + nWidth + "&GridHeight=" + nHeight;
}

// **** End of File ****
#endif

namespace WPFPipes.Engine
{
    public interface IPipesWindow
    {
        void DisplayMessage(string message1, string message2);
    }

    public class GraphicsEngine
    {
        private const int tileWidth = 32;
        private const int tileBorderWidth = 2;
        private const int pipeWidth = 8;
        private const int a1 = (tileWidth - pipeWidth) / 2;
        private const int a2 = (tileWidth + pipeWidth) / 2;
        private const int yCompensation = -1;                   // To compensate for a bug in the WriteableBitmapEx library version 1.0.3.0
        private readonly IPipesWindow window;
        public readonly WriteableBitmap bitmap;
        public readonly List<WriteableBitmap> redTileBitmapList = new List<WriteableBitmap>();
        public readonly List<WriteableBitmap> greenTileBitmapList = new List<WriteableBitmap>();
        private readonly int bitmapWidth;
        private readonly int bitmapHeight;
        private readonly int nGridWidth;
        private readonly int nGridHeight;
        private readonly int nGridArea;
        private const string playMessage1 = "Click on a square to rotate the pipe segment within it.";
        private const string playMessage2 = "The goal is to connect all of the pipe segments together, so that they all turn green.";
        private const string victoryMessage1 = "Victory!";
        private const string victoryMessage2 = "Click on any square to start a new puzzle.";
        private const int nNumDirections = 4;             // Up, right, down, and left (in clockwise order).
        private readonly List<int> aPowersOfTwo = new List<int>() { 1, 2, 4, 8 };    // aPowersOfTwo.length == nNumDirections
        private readonly List<int> adx = new List<int>() { 0, 1, 0, -1 };            // adx.length == nNumDirections
        private readonly List<int> ady = new List<int>() { -1, 0, 1, 0 };            // ady.length == nNumDirections
        private bool bVictory = false;
        private readonly List<int> aGridImageNumbers = new List<int>();
        private readonly List<bool> abImageIsGreen = new List<bool>();

        public GraphicsEngine(IPipesWindow window, int bitmapWidth, int bitmapHeight)
        {
            this.window = window;
            this.bitmapWidth = bitmapWidth;
            this.bitmapHeight = bitmapHeight;
            this.bitmap = BitmapFactory.New(this.bitmapWidth, this.bitmapHeight);
            this.nGridWidth = bitmapWidth / tileWidth;
            this.nGridHeight = bitmapHeight / tileWidth;
            this.nGridArea = this.nGridWidth * this.nGridHeight;

            for (var i = 0; i < 16; ++i)
            {
                redTileBitmapList.Add(CreateTileBitmap(i, Colors.Red));
                greenTileBitmapList.Add(CreateTileBitmap(i, Colors.Lime));
            }

            prepareForNewGame();
            //setGridDimensions();
            setGridImageNumbers();
            findGreenTree();
            Render();
        }

        private WriteableBitmap CreateTileBitmap(int n, Color colour)
        {
            var tileBitmap = BitmapFactory.New(tileWidth, tileWidth);

            tileBitmap.Clear(Colors.DarkGray);
            tileBitmap.FillRectangle(tileBorderWidth, tileBorderWidth,
                tileWidth - tileBorderWidth, tileWidth - tileBorderWidth + yCompensation, Colors.Black);

            if ((n & 1) != 0)
            {
                tileBitmap.FillRectangle(a1, 0, a2, a2 + yCompensation, colour);
            }

            if ((n & 2) != 0)
            {
                tileBitmap.FillRectangle(a1, a1, tileWidth, a2 + yCompensation, colour);
            }

            if ((n & 4) != 0)
            {
                tileBitmap.FillRectangle(a1, a1, a2, tileWidth + yCompensation, colour);
            }

            if ((n & 8) != 0)
            {
                tileBitmap.FillRectangle(0, a1, a2, a2 + yCompensation, colour);
            }

            return tileBitmap;
        }

        private void Render()
        {
            var i = 0;
            var destRect = new System.Windows.Rect();
            var sourceRect = new System.Windows.Rect(0.0, 0.0, (double)tileWidth, (double)tileWidth);

            destRect.Width = (double)tileWidth;
            destRect.Height = (double)tileWidth;

            for (var row = 0; row < this.nGridHeight; ++row)
            {
                destRect.Y = (double)(row * tileWidth);

                for (var column = 0; column < this.nGridWidth; ++column)
                {
                    var tileBitmapNumber = aGridImageNumbers[i];
                    var tileBitmap = abImageIsGreen[i] ? greenTileBitmapList[tileBitmapNumber] : redTileBitmapList[tileBitmapNumber];

                    destRect.X = (double)(column * tileWidth);
                    this.bitmap.Blit(destRect, tileBitmap, sourceRect);
                    ++i;
                }
            }

        }

        private void prepareForNewGame()
        {
            this.window.DisplayMessage(playMessage1, playMessage2);
        }

        private void createSolution()
        {
            var aBlobNumbers = new List<int>();
            var aOpenList = new List<int>();
            var openListLength = nGridArea;
            var aDirectionIndices = new int[nNumDirections];    // Number of directions == 4
            var numConnections = 0;
            var r = new Random();

            aGridImageNumbers.Clear();

            for (var i = 0; i < nGridArea; ++i)
            {
                aGridImageNumbers.Add(0);
                aBlobNumbers.Add(i);
                aOpenList.Add(i);
            }

            while (numConnections < nGridArea - 1)
            {
                // Randomly select a member of the open list.
                var openListIndex = r.Next(openListLength);
                var openListElement = aOpenList[openListIndex];
                var blobNumber1 = aBlobNumbers[openListElement];
                var row1 = openListElement / nGridWidth;
                var col1 = openListElement % nGridWidth;

                for (var i = 0; i < aDirectionIndices.Length; i++)
                {
                    aDirectionIndices[i] = i;
                }

                var connectionCreatedDuringThisPass = false;
                var numDirectionIndices = aDirectionIndices.Length;

                while (numDirectionIndices > 0 && !connectionCreatedDuringThisPass)
                {
                    var j = r.Next(numDirectionIndices);
                    var directionIndex = aDirectionIndices[j];

                    numDirectionIndices--;
                    aDirectionIndices[j] = aDirectionIndices[numDirectionIndices];

                    var dx = adx[directionIndex];
                    var dy = ady[directionIndex];
                    var row2 = row1 + dy;
                    var col2 = col1 + dx;

                    if (row2 < 0 || row2 >= nGridHeight || col2 < 0 || col2 >= nGridWidth)
                    {
                        continue;
                    }

                    var index2 = row2 * nGridWidth + col2;
                    var blobNumber2 = aBlobNumbers[index2];

                    if (blobNumber1 == blobNumber2)
                    {
                        continue;
                    }

                    // Create the new connection.

                    aGridImageNumbers[openListElement] += aPowersOfTwo[directionIndex];
                    aGridImageNumbers[index2] += aPowersOfTwo[directionIndex ^ 2];   // Question: Is ^ the bitwise XOR operator?  Yes.

                    numConnections++;
                    connectionCreatedDuringThisPass = true;

                    var minBlobNumber = Math.Min(blobNumber1, blobNumber2);
                    var maxBlobNumber = Math.Max(blobNumber1, blobNumber2);

                    for (var i = 0; i < aBlobNumbers.Count; i++)
                    {

                        if (aBlobNumbers[i] == maxBlobNumber)
                        {
                            aBlobNumbers[i] = minBlobNumber;
                        }
                    }

                    // When the grid is fully constructed, all of the blob numbers will be 0.
                    // In other words, every square in the grid will be a member of blob number 0.
                }

                if (!connectionCreatedDuringThisPass)
                {
                    // The element at (row1, col1) has no neighbour belonging to a different blob;
                    // therefore we will remove it from the open list.

                    openListLength--;
                    aOpenList[openListIndex] = aOpenList[openListLength];
                }
            }
        }

        private void randomlyRotateImages()
        {
            // Blank: 0
            // i: 1, 2, 4, 8
            // I: 5, 10
            // L: 3, 6, 9, 12
            // T: 7, 11, 13, 14
            // +: 15
            var aaRotatedIndices = new List<List<int>>() {
                new List<int>() { 0 },            // 0
                new List<int>() { 2, 4, 8 },      // 1
                new List<int>() { 1, 4, 8 },      // 2
                new List<int>() { 6, 9, 12 },     // 3
                new List<int>() { 1, 2, 8 },      // 4
                new List<int>() { 10 },           // 5
                new List<int>() { 3, 9, 12 },     // 6
                new List<int>() { 11, 13, 14 },   // 7
                new List<int>() { 1, 2, 4 },      // 8
                new List<int>() { 3, 6, 12 },     // 9
                new List<int>() { 5 },            // 10
                new List<int>() { 7, 13, 14 },    // 11
                new List<int>() { 3, 6, 9 },      // 12
                new List<int>() { 7, 11, 14 },    // 13
                new List<int>() { 7, 11, 13 },    // 14
                new List<int>() { 15 }            // 15
            };
            var r = new Random();

            for (var i = 0; i < aGridImageNumbers.Count; i++) {
                var aRotationOptions = aaRotatedIndices[aGridImageNumbers[i]];
                var j = r.Next(aRotationOptions.Count);
                var rotatedIndex = aRotationOptions[j];

                aGridImageNumbers[i] = rotatedIndex;
            }
        }

        private void setGridImageNumbers()
        {
            createSolution();
            randomlyRotateImages();
        }

        private int findGreenSubtree(int row1, int col1) {
            /* Unnecessary.
            if (row1 < 0 || row1 >= nGridHeight || col1 < 0 || col1 >= nGridWidth) {
                return 0
            }
            */

            var index1 = row1 * nGridWidth + col1;

            if (abImageIsGreen[index1]) {   // Avoid infinite loops.
                return 0;
            }

            abImageIsGreen[index1] = true;

            var numGreenImagesInSubtree = 1;
            var image1 = aGridImageNumbers[index1];

            for (var i = 0; i < nNumDirections; i++) {
                var row2 = row1 + ady[i];
                var col2 = col1 + adx[i];

                if (row2 < 0 || row2 >= nGridHeight || col2 < 0 || col2 >= nGridWidth) {
                    continue;
                }

                var index2 = row2 * nGridWidth + col2;
                var image2 = aGridImageNumbers[index2];

                if ((image1 & aPowersOfTwo[i]) != 0 && (image2 & aPowersOfTwo[i ^ 2]) != 0) {
                    // There is a connection between the square at (row1, col1) and the square at (row2, col2).
                    numGreenImagesInSubtree += findGreenSubtree(row2, col2);
                }
            }

            return numGreenImagesInSubtree;
        }

        private int findGreenTree() {
            abImageIsGreen.Clear();

            for (var i = 0; i < nGridArea; ++i)
            {
                abImageIsGreen.Add(false);
            }

            return findGreenSubtree(nGridHeight / 2, nGridWidth / 2);
        }

        public int rolNybble(int nybble)
        {
            nybble %= 16;           // For safety.  Probably unnecessary.
            nybble *= 2;

            if (nybble >= 16)       // The "carry" bit is 1.
            {
                nybble -= 16;       // To remove the bit that has been rotated into the "carry" bit.
                nybble++;           // To set bit 0 of the nybble from the "carry" bit.
            }

            return nybble;
        }

        public int setImageSourcesAfterClick()
        {
            var numGreenImages = findGreenTree();

            Render();

            return numGreenImages;
        }

        public void onCanvasClick(int x, int y)
        {

            if (bVictory)
            {
                // Start a new game.
                bVictory = false;
                prepareForNewGame();
                setGridImageNumbers();
                setImageSourcesAfterClick();
                return;
            }

            var row = y / tileWidth;
            var column = x / tileWidth;

            if (row < 0 || row >= this.nGridHeight || column < 0 || column >= this.nGridWidth)
            {
                return;
            }

            var index = row * this.nGridWidth + column;

            aGridImageNumbers[index] = rolNybble(aGridImageNumbers[index]);

            if (setImageSourcesAfterClick() == nGridArea)
            {
                bVictory = true;
                //document.bgColor = "Lime";  // "Green";
                this.window.DisplayMessage(victoryMessage1, victoryMessage2);
                //setTimeout("document.bgColor = 'White'", 3000);       //  Three-second delay
            }
        }
    }
}

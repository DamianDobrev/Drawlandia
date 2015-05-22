$(function () {
    var game = $.connection.gameHub;

    // GAME FUNCTION

    function openGame(parameters) {
        var context = document.getElementById('gameCanvas').getContext("2d");

        var paint = false;
        var mousePosX;
        var mousePosY;
        var color = "#000000";

        var clickX = new Array();
        var clickY = new Array();
        var clickDrag = new Array();
        var colors = new Array();

        function addClick(x, y, dragging, colorCur) {
            clickX.push(x);
            clickY.push(y);
            clickDrag.push(dragging);
            colors.push(colorCur);
        }

        function redraw() {
            context.clearRect(0, 0, context.canvas.width, context.canvas.height);

            context.lineJoin = "round";
            context.lineWidth = 5;

            for (var i = 0; i < clickX.length; i++) {
                context.strokeStyle = colors[i];
                context.beginPath();
                if (clickDrag[i] && i) {
                    context.moveTo(clickX[i - 1], clickY[i - 1]);
                } else {
                    context.moveTo(clickX[i] - 1, clickY[i]);
                }
                context.lineTo(clickX[i], clickY[i]);
                context.closePath();
                context.stroke();
            }
        }
        
        //mouse interaction functions

        $('#gameCanvas').mousedown(function (e) {
            var canvasPos = $('#gameCanvas').offset();
            console.log(canvasPos.left);
            mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
            mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

            paint = true;
            game.server.draw(mousePosX, mousePosY, false, color);
        });
        $('#gameCanvas').mousemove(function (e) {
            var canvasPos = $('#gameCanvas').offset();
            console.log(canvasPos.left);
            mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
            mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

            if (paint) {
                game.server.draw(mousePosX, mousePosY, true, color);
            }
        });
        $('#gameCanvas').mouseup(function (e) {
            paint = false;
        });
        $('#gameCanvas').mouseleave(function (e) {
            paint = false;
        });

        //instruments

        $('#brushColorBlack').click(function (e) {
            color = '#000000';
        });
        $('#brushColorRed').click(function (e) {
            color = '#ff0000';
        });
        $('#brushEraser').click(function (e) {
            color = '#ffffff';
        });
        $('#clearCanvas').click(function (e) {
            game.server.clear();
        });
    }

    $.connection.hub.start().done(function () {
        var name;

        //Enter name

        $('#view').load('app/templates/insertName.html', function () {
            $('#insertNameBtn').click(function () {
                var nameInput = $('#name');
                if (nameInput.val()) {
                    name = nameInput.val();
                    showRooms();
                } else {
                    $('.errorMsg').text("Name should be at least 1 character long");
                }
            });
        });

        //Show rooms when name is set

        function showRooms() {
            $('#view').load('app/templates/showRooms.html', function () {
                
            });
        }

        //Join to a room

        var roomName;
        var roomPass;

        //game.server.registerNewPlayer(name);

        // Game function

        //openGame();


        
    });

    // ----------------------------
    // functions called from server
    // ----------------------------

    game.client.drawRemote = function (xRemote, yRemote, dragRemote, colorCurRemote) {
        addClick(xRemote, yRemote, dragRemote, colorCurRemote);
        redraw();
    };

    game.client.clearCanvas = function () {
        context.clearRect(0, 0, context.canvas.width, context.canvas.height);
        clickX = new Array();
        clickY = new Array();
        clickDrag = new Array();
        colors = new Array();
    }
});
$(function () {
    var game = $.connection.gameHub;
    var rooms = $.connection.roomsHub;

    // GAME FUNCTION

    function openGame() {
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
            mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
            mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

            paint = true;
            game.server.draw(mousePosX, mousePosY, false, color);
        });
        $('#gameCanvas').mousemove(function (e) {
            var canvasPos = $('#gameCanvas').offset();
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

        //functions called by server

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
            alert("CLEAR CANVAS CALLED FROM SERVER");
        }
    }

    $.connection.hub.start().done(function () {
        var name;

        //Enter name
        $('#view').load('app/templates/insertName.html', function () {

            //events

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

                rooms.server.getAllRooms();

                //events

                $('#newRoomOpenBtn').click(function () {
                    var popup = $('#newRoomPopup');
                    if (popup.css('display') == 'none') {
                        popup.show();
                    } else {
                        popup.hide();
                    }
                });
            });
        }
    });

    //---------------
    //other functions
    //---------------

    function setRoomCounter(count) {
        $('#roomCount').text(count);
    }

    //--------------------------
    //functions called by server
    //--------------------------

    rooms.client.initializeRooms = function (roomsJson) {
        var roomsArray = JSON.parse(roomsJson);
        $('#rooms').html('');
        roomsArray.forEach(function (room) {
            $('#rooms').append($('<li>')
                .append($('<div>').text(room.Name))
                .append($('<button class="joinBtn">').text('Join')));
        });
        setRoomCounter(roomsArray.length);
    }

    
});
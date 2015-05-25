$(function () {
    var game = $.connection.gameHub;
    var rooms = $.connection.roomsHub;
    var name;

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
                    closePopups();
                }
            });

            $('.closePopupBtn').click(function () {
                closePopups();
            });

            $('#createRoomBtn').click(function () {
                var roomName = $('#roomName').val();
                var roomPass = $('#roomPass').val();

                //create room in db
                rooms.server.createRoom(roomName, roomPass, name);
            });

        });
    }

    function setRoomCounter(count) {
        $('#roomCount').text(count);
    }

    function closePopups() {
        $('.popupBody').hide();
        $('#pass').val('');
        $('#roomName').val('');
        $('#roomPass').val('');
    }

    function updatePlayerData(players) {
        $('#players ul').html('');
        players.forEach(function (player) {
            var liToAppend = $('<li><span>' + player.Score + '</span><span>' + player.Name + '</span></li>');
            if (player.IsHisTurn) {
                liToAppend.attr('class', 'isHisTurn');
            }
            $('#players ul').append(liToAppend);
        });
    }

    var previousAuthor = '';

    rooms.client.addMessage = function(author, message) {
        if (previousAuthor != author) {
            $('#chat div ul').append($('<li class="playerName">').text(author));
        }
        previousAuthor = author;
        $('#chat div ul').append($('<li>').text(message));
        $('#chat div.whiteContainer').scrollTop(1000000);
    }

    rooms.client.updatePlayers = function(players) {
        updatePlayerData(players);
    }

    rooms.client.alertNew = function () {
        alert("AlertNew func");
    }

    rooms.client.errorWithMsg = function (msg) {
        alert(msg);
        console.log(msg);
    }

    rooms.client.initializeRooms = function (roomsJson) {
        var roomsArray = JSON.parse(roomsJson);
        $('#rooms').html('');
        roomsArray.forEach(function (room) {

            //check password protection
            var hasPass = false;
            if (room.HasPassword) {
                hasPass = true;
            }

            //insert html
            $('#rooms').append($('<li>')
                .append($('<div>').text(room.Name + ' -> has pass: ' + hasPass))
                .append($('<button class="joinBtn fancyYellowBtn applyTransition" data-id="' + room.Id + '" data-has-pass="' + hasPass + '">').text('Join')));

        });

        //events
        $('.joinBtn').click(function (e) {
            var roomToJoinId = $(this).attr('data-id');
            var hasPassword = $(this).attr('data-has-pass');
            if (JSON.parse(hasPassword)) {
                var popup = $('#joinPopup');
                if (popup.css('display') == 'none') {
                    popup.show();
                }

                $('#joinToRoomWithPassword').click(function () {
                    var roomPass = $('#pass').val();

                    $('.closePopupBtn').click(function () {
                        closePopups();
                    });
                    //create room in db
                    rooms.server.joinRoom(roomToJoinId, roomPass, name);
                });
            } else {
                rooms.server.joinRoom(roomToJoinId, '', name);
            }
        });

        //set the counter value
        setRoomCounter(roomsArray.length);
    }

    rooms.client.initRoom = function(roomParams) {
        $('#view').load('app/templates/game.html', function () {

            var room = JSON.parse(roomParams);

            updatePlayerData(room.Players);

            //events

            $('#leaveRoomBtn').click(function() {
                rooms.server.leaveRoom(room.Id);
            });

            function sendMsg() {
                var message = $('#chatInput').val();
                if (message != '' && message != null) {
                    rooms.server.sendMessage(message);
                    $('#chatInput').val('');
                    $('#chatInput').focus();
                }
            }

            $('#sendMsgBtn').click(function () {
                sendMsg();
            });

            $('#chatInput').keypress(function (e) {
                var code = e.keyCode || e.which;
                if (code == 13) {
                    sendMsg();
                }
            });

            $('#newGameBtn').click(function() {

            });
        });
    }

    rooms.client.redirectToLobby = function() {
        showRooms();
    }

    rooms.client.playSound = function(type) {
        alert(type);
    }
});
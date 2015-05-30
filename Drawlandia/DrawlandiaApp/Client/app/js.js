$(function () {
    var games = $.connection.gameHub;
    var name;

    var previousAuthor = '';

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

    function showRooms() {
        $('#view').load('app/templates/showRooms.html', function () {

            games.server.getAllGames();

            //events

            $('#refreshGamesBtn').click(function (e) {
                games.server.getAllGames();
            });

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
                games.server.createGame(roomName, roomPass, name);
            });

        });
    }

    function setRoomCounter(count) {
        $('#roomCount').text(count);
    }

    window.closePopups = function () {
        $('.popupBody').hide();
        $('#pass').val('');
        $('#roomName').val('');
        $('#roomPass').val('');
    }

    function updatePlayerData(players) {
        $('#players ul').html('');
        players.forEach(function (player) {
            var liToAppend = $('<li><span>' + player.Score + '</span><span data-id="' + player.Id + '">' + player.Name + '</span></li>');
            console.log(player);
            if (player.PlayerState === 2) {
                liToAppend = $('<li><span>' + player.Score + '</span><span data-id="' + player.Id + '" class="disconnectedPlayer">' + player.Name + '</span></li>');
            }
            if (player.IsHisTurn) {
                liToAppend.attr('class', 'isHisTurn');
            }
            $('#players ul').append(liToAppend);
        });
    }

    games.client.initializeGames = function (roomsJson) {
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
            var gameToJoinId = $(this).attr('data-id');
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

                    games.server.joinGame(gameToJoinId, roomPass, name);
                });
            } else {
                games.server.joinGame(gameToJoinId, '', name);
            }
        });

        //set the counter value
        setRoomCounter(roomsArray.length);
    }

    games.client.initGame = function(gameParams) {
        $('#view').load('app/templates/game.html', function () {

            var game = JSON.parse(gameParams);

            updatePlayerData(game.Players);

            $('#gameName').text(game.Name);


            $(function () {
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

                $('#gameCanvas').mousedown(function(e) {
                    var canvasPos = $('#gameCanvas').offset();
                    mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
                    mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

                    paint = true;
                    games.server.draw(mousePosX, mousePosY, false, color);
                });
                $('#gameCanvas').mousemove(function(e) {
                    var canvasPos = $('#gameCanvas').offset();
                    mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
                    mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

                    if (paint) {
                        games.server.draw(mousePosX, mousePosY, true, color);
                    }
                });
                $('#gameCanvas').mouseup(function(e) {
                    paint = false;
                });
                $('#gameCanvas').mouseleave(function(e) {
                    paint = false;
                });

                //instruments

                $('#brushColorBlack').click(function(e) {
                    color = '#000000';
                });
                $('#brushColorRed').click(function(e) {
                    color = '#ff0000';
                });
                $('#brushEraser').click(function(e) {
                    color = '#ffffff';
                });
                $('#clearCanvas').click(function(e) {
                    games.server.clear();
                });

                //functions called by server

                games.client.drawRemote = function (xRemote, yRemote, dragRemote, colorCurRemote) {
                    addClick(xRemote, yRemote, dragRemote, colorCurRemote);
                    redraw();
                };

                games.client.clearCanvas = function () {
                    context.clearRect(0, 0, context.canvas.width, context.canvas.height);
                    clickX = new Array();
                    clickY = new Array();
                    clickDrag = new Array();
                    colors = new Array();
                    alert("CLEAR CANVAS CALLED FROM SERVER");
                }
            });



            //events

            $('#leaveRoomBtn').click(function() {
                games.server.leaveGame(game.Id);
            });

            function sendMsg() {
                var message = $('#chatInput').val();
                if (message != '' && message != null) {
                    games.server.sendMessage(message);
                    $('#chatInput').val('');
                }
                $('#chatInput').focus();
            }

            $('#sendMsgBtn').click(function () {
                sendMsg();
            });

            $('#chatInput').keypress(function (e) {
                var code = e.keyCode || e.which;
                //If key is Enter
                if (code == 13) {
                    sendMsg();
                }
            });

            $('#newGameBtn').click(function() {
                games.server.startGame();
            });
        });
    }

    games.client.updatePlayers = function (players) {
        updatePlayerData(players);
    }

    games.client.redirectToLobby = function() {
        showRooms();
    }

    games.client.playSound = function(type) {
        alert(type);
    }

    games.client.addMessage = function (author, message) {
        if (previousAuthor !== author) {
            $('#chat div ul').append($('<li class="playerName">').text(author));
        }
        previousAuthor = author;
        $('#chat div ul').append($('<li>').text(message));
        $('#chat div.whiteContainer').scrollTop(1000000);
    }

    games.client.errorWithMsg = function (msg) {
        alert('errorWithMsg(): ' + msg);
        console.log(msg);
    }

    games.client.onGuessedWord = function(msg, players) {
        alert('onGuessedWord(): ' + msg);
        updatePlayerData(players);
    }

    games.client.becomeDrawer = function (word) {
        $('#currentWord').show().html('<span class="swd">Draw:</span> ' + word);
        $('#currentPattern').text('').hide();
    }

    games.client.becomeGuesser = function (pattern) {
        $('#currentPattern').show().html('<span class="swd">Guess:</span> ' + pattern);
        $('#currentWord').text('').hide();
    }

    games.client.becomeOrdinaryPlayer = function() {
        $('#newGameBtn').hide();
    }

    games.client.becomeOwner = function () {
        $('#newGameBtn').show();
    }

    games.client.gameOver = function(msg) {
        alert(msg);
    }

    games.client.cutLegs = function() {
        $('#leaveRoomBtn').hide();
        $('#newGameBtn').hide();
        $('#timer').show();
    }

    games.client.unlockCanvas = function() {
        
    }

    games.client.lockCanvas = function() {
        
    }
});
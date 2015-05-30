$(function () {
    var games = $.connection.gameHub;

    var name;
    var gameName;
    var isDrawer = false;

    var context;
    var clickX = new Array();
    var clickY = new Array();
    var clickDrag = new Array();
    var colors = new Array();
    var sizes = new Array();

    var paint = false;
    var mousePosX;
    var mousePosY;
    var brColor = "#000000";
    var brSize = 4;

    var previousAuthor = '';

    window.setBrColor = function (hexColor)
    {
        brColor = '#' + hexColor;
        $('#brushColors li button').each(function() {
            $(this).removeClass('selectedClr');
        });
    }

    window.setBrSize = function(size) {
        brSize = size;
        $('#brushSize li button').each(function () {
            $(this).removeClass('selectedSize');
        });
    }

    window.setContext = function (contextValue) {
        context = contextValue;
    }

    window.setGameName = function (gName) {
        gameName = gName;
        $('#gameName').text(gName);
    }

    window.setDrawer = function (drawer) {
        isDrawer = drawer;
    }

    $.connection.hub.start().done(function () {
        //Enter name
        $('#view').load('app/templates/insertName.html', function () {
            $('#insertNameBtn').click(function () {
                var nameInput = $('#name');
                if (nameInput.val()) {
                    name = nameInput.val();
                    initLobby();
                } else {
                    $('.errorMsg').text("Name should be at least 1 character long");
                }
            });
        });
    });

    function initLobby() {
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

    games.client.initGame = function (gameParams, brushColors, brushSizes) {
        $('#view').load('app/templates/game.html', function () {

            var game = JSON.parse(gameParams);

            setGameName(game.Name);

            setContext(document.getElementById('gameCanvas').getContext("2d"));

            updatePlayerData(game.Players);

            initBrushColors(brushColors);

            initBrushSizes(brushSizes);

            $('#clearCanvas').click(function (e) {
                games.server.clear();
                alert("DSADSA");
            });
            //events

            $('#leaveRoomBtn').click(function () {
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

            $('#newGameBtn').click(function () {
                games.server.startGame();
            });
        });
    }

    window.initBrushColors = function (brushColors) {
        var brushColorsObj = JSON.parse(brushColors);
        brushColorsObj.forEach(function (colorObj) {
            var color = colorObj.ColorHex;
            $('#brushColors')
                .append($('<li>')
                    .append($('<button class="colorButton">')
                        .css('background-color', '#' + color)
                        .attr('data-color', color)));

        });

        $('#brushColors li:last button').addClass('selectedClr');

        $('.colorButton').click(function (e) {
            var color = $(this).attr('data-color');
            setBrColor(color);
            $(this).addClass('selectedClr');
        });
    }

    window.initBrushSizes = function (brushSizes) {
        var brushSizesObj = JSON.parse(brushSizes);
        brushSizesObj.forEach(function (sizeObj) {
            var size = sizeObj.Size;
            $('#brushSize')
                .append($('<li>')
                    .append($('<button class="sizeButton">')
                        .css('width', size)
                        .css('height', size)
                        .css('border-radius', size)
                        .attr('data-size', size)));

        });

        $('#brushSize li:nth-child(2) button').addClass('selectedSize');

        $('#brushSize li').click(function (e) {
            var size = $(this).find('button').attr('data-size');
            setBrSize(size);
            $(this).find('button').addClass('selectedSize');
        });
    }

    window.addClick = function (x, y, dragging, colorCur, sizeCur) {
        clickX.push(x);
        clickY.push(y);
        clickDrag.push(dragging);
        colors.push(colorCur);
        sizes.push(sizeCur);
    }

    window.redraw = function () {
        context.clearRect(0, 0, context.canvas.width, context.canvas.height);

        context.lineJoin = "round";

        for (var i = 0; i < clickX.length; i++) {
            context.lineWidth = sizes[i];
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

    games.client.updatePlayers = function (players) {
        updatePlayerData(players);
    }

    games.client.redirectToLobby = function () {
        initLobby();
    }

    games.client.playSound = function (type) {
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

    games.client.onGuessedWord = function (msg, players) {
        alert('onGuessedWord(): ' + msg);
        updatePlayerData(players);
    }

    games.client.becomeDrawer = function (word) {
        $('#currentWord').show().html('<span class="swd">Draw:</span> ' + word);
        $('#currentPattern').text('').hide();

        setDrawer(true);

        // mouse-canvas interaction

        $('#gameCanvas').mousedown(function (e) {
            if (!isDrawer) {
                return;
            }
            var canvasPos = $('#gameCanvas').offset();
            mousePosX = e.pageX - Math.round(canvasPos.left);
            mousePosY = e.pageY - Math.round(canvasPos.top);

            paint = true;
            paintCanvas(mousePosX, mousePosY, false, brColor, brSize);
            games.server.draw(gameName, mousePosX, mousePosY, false, brColor, brSize);
        });

        $('#gameCanvas').mousemove(function (e) {
            if (!isDrawer) {
                return;
            }
            var canvasPos = $('#gameCanvas').offset();
            mousePosX = e.pageX - Math.round(canvasPos.left);
            mousePosY = e.pageY - Math.round(canvasPos.top);

            if (paint) {
                paintCanvas(mousePosX, mousePosY, true, brColor, brSize);
                games.server.draw(gameName, mousePosX, mousePosY, true, brColor, brSize);
            }
        });

        $('#gameCanvas').mouseup(function (e) {
            if (!isDrawer) {
                return;
            }
            paint = false;
        });

        $('#gameCanvas').mouseleave(function (e) {
            if (!isDrawer) {
                return;
            }
            paint = false;
        });
    }

    games.client.becomeGuesser = function (pattern) {
        $('#currentPattern').show().html('<span class="swd">Guess:</span> ' + pattern);
        $('#currentWord').text('').hide();
        setDrawer(false);
    }

    games.client.becomeOrdinaryPlayer = function () {
        $('#newGameBtn').hide();
    }

    games.client.becomeOwner = function () {
        $('#newGameBtn').show();
    }

    games.client.gameOver = function (msg, id) {
        alert(msg);
        games.server.GoToGame(id);
    }

    games.client.cutLegs = function () {
        $('#leaveRoomBtn').hide();
        $('#newGameBtn').hide();
        $('#timer').show();
    }

    games.client.drawRemote = function (xRemote, yRemote, dragRemote, colorCurRemote, sizeCur) {
        paintCanvas(xRemote, yRemote, dragRemote, colorCurRemote, sizeCur);
    };

    window.paintCanvas = function (xRemote, yRemote, dragRemote, colorCurRemote, sizeCur) {
        addClick(xRemote, yRemote, dragRemote, colorCurRemote, sizeCur);
        redraw();
    };

    games.client.clearCanvas = function () {
        clearCanvasLocal();
    }

    window.clearCanvasLocal = function () {
        context.clearRect(0, 0, context.canvas.width, context.canvas.height);
        clickX = new Array();
        clickY = new Array();
        clickDrag = new Array();
        colors = new Array();
        sizes = new Array();
    }
});
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
            var liToAppend = $('<li><span>' + player.Score + '</span><span>' + player.Name + '</span></li>');
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
        if (previousAuthor != author) {
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

    games.client.onGuessedWord = function(msg) {
        alert('onGuessedWord(): ' + msg);
    }

    games.client.becomeDrawer = function (word) {
        alert("I am drawer of this word: " + word);
    }

    games.client.becomeGuesser = function (pattern) {
        alert("I am guesser of this pattern: " + pattern);
    }
});
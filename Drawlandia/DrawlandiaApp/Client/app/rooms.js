$(function () {
    var rooms = $.connection.roomsHub;

    $.connection.hub.start().done(function () {
        rooms.server.getRooms();
    });

    // ----------------------------
    // functions called from server
    // ----------------------------

    rooms.client.initializeRooms = function (roomsAsJson) {
        var roomsArr = JSON.parse(roomsAsJson);

        roomsArr.forEach(function(room) {
            $('#roomsContainer')
                .append($('<li>')
                    .append($('<div class="roomName">').text(room.Name))
                    .append($('<div class="roomJoinButtonHolder">')
                        .append($('<button class="joinButton" data-id='+ room.Id +'>').text('Join'))));

            $('.joinButton').click(function (e) {
                alert("asd");
            });
        });
    };

});
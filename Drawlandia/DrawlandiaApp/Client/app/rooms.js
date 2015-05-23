$(function () {
    var rooms = $.connection.roomsHub;

    $.connection.hub.start().done(function () {
        rooms.server.getAllRooms();
    });

    // ----------------------------
    // functions called from server
    // ----------------------------

    rooms.client.initializeRooms = function (roomsAsJson) {
        var roomsArr = JSON.parse(roomsAsJson);

        alert(roomsArr);
    };

});
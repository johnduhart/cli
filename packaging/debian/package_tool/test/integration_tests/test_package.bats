#!/bin/bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

#Ensure running with superuser privileges
current_user=$(whoami)
if [ $current_user != "root" ]; then
	echo "WARNING: test_package.sh requires superuser privileges to run"
	SUDO_PREFIX="sudo"
fi

setup(){
	DIR="$BATS_TEST_DIRNAME"

	PACKAGE_FILENAME="$(ls $DIR | grep .deb -m 1)"
	PACKAGE_PATH="$DIR/*.deb"

	# Get Package name from package path, 
	PACKAGE_NAME=${PACKAGE_FILENAME%%_*}
}

install_package(){
	$SUDO_PREFIX dpkg -i $PACKAGE_PATH
}

remove_package(){
	$SUDO_PREFIX dpkg -r $PACKAGE_NAME
}

purge_package(){
	$SUDO_PREFIX dpkg -P $PACKAGE_NAME
}

@test "package install + removal test" {
	install_package
	remove_package
}

@test "package install + purge test" {
	install_package
	purge_package
}

# Ultimate Package Test
# https://www.debian.org/doc/manuals/maint-guide/checkit.en.html#pmaintscripts
@test "package install + upgrade + purge + install + remove + install + purge test" {
	# TODO: need to figure out how to mock upgrades
}

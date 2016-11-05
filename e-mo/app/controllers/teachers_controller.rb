class TeachersController < ApplicationController
  before_action :access_filter

  def index
  end

  private
    def access_filter
      if current_user.teacher_flag
        # 先生でログイン
      else
        # 生徒でログイン
        redirect_to student_url
      end
    end
end
